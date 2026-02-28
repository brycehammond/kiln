# Voice Input/Output Systems Analysis for Windows Desktop Application

## Executive Summary

This document analyzes voice I/O technologies for building a voice-driven Unity development assistant targeting Windows desktop. The recommended stack is: **Deepgram Nova-3** for streaming STT, **Cartesia Sonic 3** (or ElevenLabs Flash) for TTS, **Picovoice Porcupine** for wake word detection, **Silero VAD** for voice activity detection, and **Python** as the voice pipeline runtime connecting to **Claude API** for natural language understanding.

---

## 1. Speech-to-Text (STT)

### Option Comparison

| Solution | Accuracy (WER) | Latency | Cost | Offline | Streaming | Notes |
|----------|----------------|---------|------|---------|-----------|-------|
| **Deepgram Nova-3** | 5.3-6.8% | <300ms TTFT | $0.0077/min streaming | No (cloud) | Yes (WebSocket) | Best balance of accuracy + latency |
| **OpenAI Whisper (local)** | ~10.6% | 1-5s (batch) | Free (self-hosted) | Yes | No (needs chunking) | Great offline fallback |
| **whisper.cpp** | Same as Whisper | 0.5-2s with GPU | Free | Yes | Partial (loop mode) | C++ port, GPU accelerated |
| **Azure Speech Services** | ~8% | 300-500ms | $1.00/hr streaming | No | Yes | 140+ languages, enterprise |
| **GPT-4o-transcribe** | Best overall | 200-400ms | $0.006/min | No | Yes | Highest accuracy but expensive |
| **Vosk** | 12-15% | <100ms | Free | Yes | Yes | Ultra-lightweight (50MB models) |
| **AssemblyAI Universal-2** | ~8.4% | 300ms | $0.0065/min | No | Yes | 30% fewer hallucinations vs Whisper |

### Recommendation: Deepgram Nova-3 (Primary) + whisper.cpp (Fallback)

**Why Deepgram Nova-3:**
- Sub-300ms time-to-first-token via WebSocket streaming
- 54.3% lower WER than competitors in streaming mode
- Cost-effective at $0.0077/min (~$0.46/hr)
- Excellent noise handling
- Simple WebSocket API for real-time streaming

**Why whisper.cpp as fallback:**
- Runs entirely offline on user hardware
- GPU acceleration via Vulkan (Windows), delivering 12x speedup on integrated GPUs
- whisper.unity package exists for direct Unity integration if needed
- Free, no API costs

**Not recommended:**
- Vosk: Accuracy too low for development commands and technical vocabulary
- Azure: More expensive, no significant advantage over Deepgram for this use case
- Raw Whisper Python: No native streaming, high latency for real-time use

---

## 2. Text-to-Speech (TTS)

### Option Comparison

| Solution | Naturalness | Latency (TTFA) | Cost | Offline | Streaming | Notes |
|----------|-------------|-----------------|------|---------|-----------|-------|
| **Cartesia Sonic 3** | Very high | 40-90ms | Usage-based | No | Yes | Fastest TTFA in industry |
| **ElevenLabs Flash 2.5** | Highest | 100-150ms | ~$0.03/1K chars | No | Yes (WebSocket) | Best voice quality + emotion |
| **OpenAI TTS** | Good | 200ms | $0.015/1K chars | No | Yes | Lower naturalness scores |
| **Piper** | Good (neural) | <50ms local | Free | Yes | No (batch) | ONNX models, lightweight |
| **Azure Neural TTS** | High | 100-200ms | $0.016/1M chars | No | Yes | Enterprise, custom voices |
| **Windows SAPI** | Low | <10ms | Free | Yes | Yes | Robotic, poor quality |

### Recommendation: Cartesia Sonic 3 (Primary) + Piper (Offline Fallback)

**Why Cartesia Sonic 3:**
- Industry-leading 40ms time-to-first-audio -- critical for natural conversation feel
- Supports AI-generated laughter and emotion
- 42 languages supported
- Fine-grained control over volume, speed, and emotion
- Purpose-built for real-time voice AI and gaming applications
- 4x faster than the next best alternative

**Why ElevenLabs as alternative:**
- Highest naturalness and emotional depth (82% pronunciation accuracy)
- Superior context awareness (63.4% vs OpenAI's 39.3%)
- WebSocket streaming support
- More expensive but best voice quality available
- Flash 2.5 model achieves sub-100ms TTFB

**Why Piper as offline fallback:**
- Fast local neural TTS with natural-sounding voices
- ONNX-based, runs on CPU with minimal resources
- pip-installable (`piper-tts`)
- Multiple quality levels (x_low to high)
- No internet required after model download

**Not recommended:**
- Windows SAPI: Too robotic for a friendly assistant
- OpenAI TTS: Lower naturalness scores, higher latency than Cartesia/ElevenLabs

---

## 3. Wake Word Detection

### Recommendation: Picovoice Porcupine

**Why Porcupine:**
- 97%+ detection accuracy with <1 false alarm per 10 hours
- Custom wake words trained in seconds via web console (free)
- Ultra-lightweight: ~1MB RAM, <4% single CPU core
- Python SDK (`pvporcupine`) available on PyPI, latest v4.0.2
- Runs on Windows, macOS, Linux, Raspberry Pi
- Free tier available with no credit card required
- Can run multiple wake words across languages simultaneously

**Implementation approach:**
- Custom wake word like "Hey Dev" or "Unity" or a project-specific name
- Always-listening mode with minimal resource usage
- Transitions to full STT pipeline upon wake word detection
- Consider also supporting push-to-talk (keyboard shortcut) as alternative activation

**Wake word vs push-to-talk trade-offs:**

| Approach | Pros | Cons |
|----------|------|------|
| Wake word | Hands-free, natural | Occasional false positives, always uses mic |
| Push-to-talk | Precise control, no false triggers | Requires keyboard/mouse, breaks flow |
| Hybrid | Best of both worlds | Slightly more complex to implement |

**Recommendation: Hybrid approach** -- support both wake word and push-to-talk (e.g., holding a configurable hotkey). Let the user choose their preferred mode. This is especially important for ADHD-friendly UX where users may prefer different interaction modes at different times.

---

## 4. Voice Activity Detection (VAD)

### Recommendation: Silero VAD

**Why Silero VAD over WebRTC VAD:**

| Metric | Silero VAD | WebRTC VAD |
|--------|-----------|------------|
| True Positive Rate (at 5% FPR) | 87.7% | 50% |
| Error rate comparison | Baseline | 4x more errors |
| Architecture | Deep neural network (PyTorch) | GMM-based signal processing |
| Processing time | <1ms per 30ms chunk (CPU) | <0.1ms per chunk |
| Model format | PyTorch / ONNX | Native C |

**Key capabilities:**
- Detects End-of-Utterance (EOU) events to trigger response pipeline
- ONNX runtime can run 4-5x faster than PyTorch in some conditions
- Handles background noise, music, and non-speech sounds well
- Configurable silence threshold (typically 400-800ms) for turn detection

**Implementation pattern:**
```
Audio Stream -> Silero VAD -> Speech detected?
  -> Yes: Buffer audio, send to STT
  -> No (silence > threshold): Finalize utterance, trigger response
```

**Tuning for ADHD-friendly interaction:**
- Use a longer silence threshold (800-1200ms) to accommodate pauses and thinking time
- Don't cut off users who pause mid-thought
- Consider prosodic features (pitch drop) in addition to silence duration

---

## 5. Conversation Flow Design

### Turn-Taking Architecture

```
[Microphone] -> [VAD] -> [STT (streaming)] -> [Claude API] -> [TTS (streaming)] -> [Speaker]
                  |                                                    |
                  +-- Barge-in detection ---> Interrupt TTS playback --+
```

### Key Design Patterns

**Streaming pipeline (reduces perceived latency):**
1. STT transcribes audio in chunks as user speaks
2. When VAD detects end-of-utterance, full transcript sent to Claude
3. Claude streams response tokens
4. TTS begins synthesizing from first sentence fragment
5. Audio playback starts before full response is generated

**Interruption handling (barge-in):**
- Monitor VAD during TTS playback
- Distinguish genuine interruptions from acknowledgments ("uh-huh", "okay")
- On valid interruption: stop TTS playback, capture new utterance
- On backchannel: continue current response

**Turn eagerness configuration:**
- Silence threshold: 500ms default, configurable 300-1500ms
- Longer thresholds for users who pause while thinking (ADHD-friendly)
- Shorter thresholds for rapid-fire command sequences

**Context management:**
- Maintain conversation history in Claude API messages array
- Include relevant project context (current file, recent errors) as system messages
- Summarize long conversations to stay within context window
- Tag conversation segments (command vs discussion vs clarification)

---

## 6. ADHD-Friendly Voice UX

### Core Principles

Based on research into inclusive conversational UI design for ADHD users:

**Patience and flexibility:**
- Longer silence thresholds (don't rush users)
- Accept rephrased or incomplete commands
- Don't require exact syntax -- use Claude's NLU to interpret intent
- Allow users to correct themselves mid-sentence

**Reducing cognitive load:**
- Keep responses concise and actionable
- Offer to break complex tasks into steps
- Provide clear confirmation of understood intent before executing
- Avoid overwhelming with options -- suggest one recommended action

**Handling tangents gracefully:**
- Claude can maintain conversation context across topic changes
- Gently redirect: "I noted that. Should I do X first, or would you like to talk about Y?"
- Allow "bookmarking" of tangent topics for later
- Don't penalize non-linear conversation flow

**Sensory considerations:**
- Configurable voice speed and pitch
- Option to adjust response verbosity (terse vs detailed)
- Subtle audio cues for state changes (listening, processing, speaking)
- Avoid startling sounds or abrupt volume changes
- Calm, consistent voice persona

**Engagement support:**
- Gentle reminders if a task was started but not completed
- "Where were we?" recovery after interruptions
- Progress summaries for multi-step tasks
- Positive reinforcement without being patronizing

**Configurable interaction modes:**
- Voice-only, text-only, or hybrid
- Push-to-talk vs wake word vs always-listening
- Adjustable response detail level
- Option to repeat or rephrase last response

---

## 7. Technology Stack Recommendation

### Recommended: Python for Voice Pipeline

| Language | Audio Libraries | Async Support | AI/ML Ecosystem | Voice Libraries | Verdict |
|----------|----------------|---------------|------------------|-----------------|---------|
| **Python** | pyaudio, sounddevice | asyncio, native | Best (PyTorch, ONNX) | Richest ecosystem | **Recommended** |
| Node.js | limited (no Web Audio API) | Event loop | Growing | Moderate | Good for web |
| C# | NAudio, Azure SDK | async/await | Limited | Azure-focused | Unity integration |

**Why Python wins:**
- Richest ecosystem for voice AI: Silero VAD, Whisper, Piper, Deepgram SDK, Picovoice SDK
- Native asyncio for concurrent audio processing (mic capture + STT + TTS playback)
- PyTorch/ONNX runtime for local ML models (VAD, offline STT)
- All recommended components have first-class Python SDKs
- FastAPI/WebSocket support for connecting to Unity
- Extensive community examples and tutorials for voice assistant pipelines

**Audio library choice: sounddevice**
- Simpler API than pyaudio
- Better async/callback support
- Built on PortAudio (cross-platform)
- 16kHz mono recommended for STT input

**Architecture: Python voice server + Unity client**
```
Unity Editor (C#)                    Python Voice Server
+------------------+                +------------------------+
| Voice UI Plugin  | <--WebSocket-->| Audio Pipeline Manager |
| - Record button  |                | - Mic capture (sounddevice)
| - Status display |                | - VAD (Silero)         |
| - Audio playback |                | - STT (Deepgram WS)   |
+------------------+                | - Claude API           |
                                    | - TTS (Cartesia WS)   |
                                    | - Wake word (Porcupine)|
                                    +------------------------+
```

Alternatively, the Python voice server could run as a standalone desktop process alongside Unity, communicating via localhost WebSocket or named pipes.

---

## 8. Integration with Claude API

### Voice Loop Architecture

```python
# Pseudocode for the core voice loop
async def voice_loop():
    while True:
        # 1. Wait for activation (wake word or push-to-talk)
        await wait_for_activation()

        # 2. Capture audio with VAD
        audio_chunks = await capture_until_silence(
            vad=silero_vad,
            silence_threshold_ms=800
        )

        # 3. Stream audio to STT
        transcript = await deepgram_streaming_stt(audio_chunks)

        # 4. Send to Claude with context
        response_stream = claude.messages.create(
            model="claude-sonnet-4-5-20250929",
            messages=conversation_history + [{"role": "user", "content": transcript}],
            system=build_system_prompt(unity_context),
            stream=True
        )

        # 5. Stream response to TTS with chunked playback
        async for text_chunk in sentence_chunker(response_stream):
            audio = await cartesia_streaming_tts(text_chunk)
            await play_audio(audio)  # Start playback immediately

            # 6. Check for barge-in during playback
            if await check_barge_in(vad):
                stop_playback()
                break
```

### Claude API Integration Details

**Model selection:**
- Use Claude Sonnet 4.5 for balanced speed/quality in voice interactions
- Streaming responses (`stream=True`) to minimize time-to-first-word
- System prompt includes Unity project context, current scene info, recent errors

**Context management:**
- Maintain sliding window of conversation history
- Inject Unity Editor state as system context (current selection, console errors, project structure)
- Use tool_use for structured commands (create object, modify component, run build)

**Response formatting for TTS:**
- Instruct Claude to respond conversationally (no markdown, no code blocks in voice responses)
- For code-heavy responses, speak a summary and display code in Unity Editor panel
- Keep spoken responses under 3-4 sentences for ADHD-friendly interaction
- Use sentence boundaries for TTS chunking

---

## 9. Lessons from Existing Voice Assistant Frameworks

### OVOS (Open Voice OS) / Mycroft

**Architecture lessons:**
- Modular plugin architecture for swappable STT/TTS/Wake Word engines is essential
- Message bus pattern (similar to pub/sub) for inter-component communication
- Skills framework with intents, dialogs, and slot filling

**Key failure lessons from Mycroft:**
- Dependence on proprietary cloud services was fatal -- always have local fallbacks
- Community rebuilt the entire stack to be local-first after Mycroft Inc. closed in 2023
- Lesson: Design for offline-first, cloud-enhanced

**Performance benchmarks (OVOS on modest hardware):**
- 700-1200ms per conversational turn with Whisper small + Piper
- Reduced to 500-900ms with streaming ASR and pre-warmed TTS
- Our cloud-based approach (Deepgram + Cartesia) should achieve 300-500ms

### Rhasspy

**Relevant patterns:**
- Slot/intent system for predictable command phrases
- Satellite microphone architecture (multiple inputs to central server)
- MQTT-based communication between components
- Excellent for structured commands but limited for open-ended conversation

### Home Assistant Voice

**Relevant patterns:**
- Wyoming protocol for standardized voice component communication
- Pipeline concept: wake word -> STT -> intent -> TTS
- Local processing emphasis for privacy

### Lessons Applied to Our Design

1. **Plugin architecture**: Make each component (STT, TTS, VAD, Wake Word) swappable
2. **Offline fallbacks**: Always have local alternatives (whisper.cpp, Piper)
3. **Message bus**: Use async event system for component communication
4. **Pipeline pattern**: Clear, composable stages with well-defined interfaces
5. **Don't over-couple**: Each component should be independently testable/replaceable

---

## 10. Latency Optimization Strategy

### Target: <500ms Time-to-First-Audio-Response

**Latency budget breakdown:**

| Stage | Target | Technique |
|-------|--------|-----------|
| VAD end-of-speech detection | 50-100ms | Silero VAD, 800ms silence threshold |
| STT finalization | 100-200ms | Deepgram streaming (already transcribing) |
| Claude API TTFT | 100-300ms | Streaming, Sonnet model, warm connection |
| TTS TTFA | 40-90ms | Cartesia Sonic 3 streaming |
| Audio playback start | <10ms | Pre-initialized audio output |
| **Total** | **300-700ms** | |

### Optimization Techniques

**1. Streaming STT (transcribe while speaking):**
- Deepgram WebSocket connection stays open during conversation
- Partial transcripts available in real-time
- Final transcript ready within 100-200ms of silence detection

**2. Speculative processing:**
- Begin Claude API call with partial transcript (update if needed)
- Pre-warm TTS connection before Claude response starts
- Keep WebSocket connections alive between turns

**3. Chunked TTS playback:**
- Split Claude response at sentence boundaries
- Begin TTS synthesis on first sentence while Claude generates rest
- Start audio playback on first TTS chunk (~40ms with Cartesia)
- Decode and play audio in 3-frame slices

**4. Connection management:**
- Keep persistent WebSocket connections to Deepgram and Cartesia
- Use connection pooling for Claude API
- Avoid cold starts by pre-warming all services on application launch

**5. Audio pipeline optimization:**
- Use 16kHz mono for STT input (sufficient quality, lower bandwidth)
- Use 22kHz for TTS output (good quality, fast synthesis)
- Buffer audio in memory, avoid disk I/O in the hot path
- Pre-initialize audio output device

**6. Reducing perceived latency:**
- Play a subtle "thinking" sound after user stops speaking
- Use backchanneling ("Got it", "Let me check") for longer operations
- Show visual feedback in Unity Editor (processing indicator)
- Begin non-audio responses immediately (e.g., highlight relevant file)

---

## Cost Estimation

### Per-Hour Usage (Estimated for Active Development Session)

| Component | Usage | Cost |
|-----------|-------|------|
| Deepgram Nova-3 STT | ~15 min actual speech/hr | ~$0.12/hr |
| Cartesia Sonic 3 TTS | ~10 min output/hr | ~$0.10/hr |
| Claude API (Sonnet) | ~50 requests/hr | ~$0.50/hr |
| Picovoice Porcupine | Always-on (free tier) | $0.00/hr |
| **Total** | | **~$0.72/hr** |

### Monthly Estimate (40hr/week active use)

- ~$115/month for an active developer
- Can reduce with offline fallbacks (Whisper + Piper) during non-critical tasks

---

## Recommended Implementation Phases

### Phase 1: Core Pipeline (MVP)
- Push-to-talk activation (skip wake word initially)
- Deepgram streaming STT
- Claude API integration with basic Unity context
- Cartesia Sonic 3 TTS with streaming playback
- Basic conversation history

### Phase 2: Enhanced Interaction
- Picovoice Porcupine wake word detection
- Silero VAD for automatic end-of-utterance detection
- Barge-in support (interrupt TTS with new speech)
- Longer conversation context with summarization

### Phase 3: ADHD-Friendly Polish
- Configurable silence thresholds and response verbosity
- Multiple interaction modes (voice/text/hybrid)
- Tangent handling and "where were we?" recovery
- Subtle audio cues and visual feedback
- Offline fallback mode (whisper.cpp + Piper)

### Phase 4: Advanced Features
- Voice persona customization
- Multi-turn task tracking with progress updates
- Proactive suggestions based on Unity Editor state
- Voice-driven code review and debugging

---

## Key Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Cloud service downtime | No voice I/O | Offline fallbacks (whisper.cpp + Piper) |
| High latency spikes | Breaks conversation flow | Connection pooling, warm connections, timeout handling |
| Background noise interference | Poor STT accuracy | Deepgram noise handling, configurable noise gate |
| API cost overruns | Budget exceeded | Usage monitoring, offline mode for non-critical tasks |
| User frustration with errors | Abandonment | Graceful error recovery, text fallback, patience in UX |
| Privacy concerns (cloud audio) | User distrust | Transparent data handling, offline mode option |

---

## Summary of Recommendations

| Component | Primary | Fallback | Why |
|-----------|---------|----------|-----|
| **STT** | Deepgram Nova-3 | whisper.cpp (local) | Best latency + accuracy combo |
| **TTS** | Cartesia Sonic 3 | Piper (local) | 40ms TTFA, emotion support |
| **Wake Word** | Picovoice Porcupine | Push-to-talk hotkey | 97% accuracy, free, lightweight |
| **VAD** | Silero VAD | -- | 4x fewer errors than WebRTC |
| **NLU** | Claude API (Sonnet) | -- | Best reasoning for dev tasks |
| **Runtime** | Python (asyncio) | -- | Richest voice AI ecosystem |
| **Audio** | sounddevice | pyaudio | Simpler async API |
