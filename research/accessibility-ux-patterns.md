# Accessibility & UX Patterns for Autism + ADHD Development Tools

Research summary for the Unity Dev Framework, focused on designing a development
environment that works with neurodiverse cognition rather than against it.

---

## Table of Contents

1. [Cognitive Load Reduction](#1-cognitive-load-reduction)
2. [Executive Function Support](#2-executive-function-support)
3. [Sensory Considerations](#3-sensory-considerations)
4. [Predictability for Autism](#4-predictability-for-autism)
5. [ADHD Engagement](#5-adhd-engagement)
6. [Voice-First UX for Neurodiverse Users](#6-voice-first-ux-for-neurodiverse-users)
7. [Existing Research & Standards](#7-existing-research--standards)
8. [Actionable Design Principles for This Framework](#8-actionable-design-principles-for-this-framework)

---

## 1. Cognitive Load Reduction

### The Problem

ADHD and autism both involve differences in how working memory, attention, and
information processing operate. Traditional development tools present dense,
visually complex interfaces that demand high sustained attention and rapid
context-switching -- exactly the areas where neurodiverse users face challenges.

### Key Patterns

**Progressive Disclosure**
- Show only the most important options initially; reveal complexity on demand.
- Limit disclosure to two levels maximum (deeper nesting causes users to get lost).
- Use accordions, expandable sections, and modal windows to hide advanced features.
- The framework should start with the simplest possible view and let users drill down.

**Chunked Information**
- Break content into small, digestible units -- one idea per section.
- Use short sentences (one conjunction max, two commas max per sentence).
- Keep paragraphs short; use bullet points and visual separators.
- Summaries for any content exceeding 200 words.

**Visual Clarity**
- Eliminate extraneous UI elements that compete for attention.
- Use high-readability sans-serif fonts with generous line spacing.
- White space between sections is critical for scannability.
- Position critical features above the scroll line ("above the fold").

**Reduced Decision Burden**
- Minimize the number of choices presented at any given time.
- Provide sensible defaults so users can proceed without configuration.
- Use "recommended" labels to guide choices without removing options.

### Framework Application

- The AI assistant should present one step at a time, not dump a full task list.
- Configuration and project settings should use progressive disclosure: simple
  mode by default, advanced options hidden behind explicit expansion.
- Code generation should present the result with a brief summary, with details
  available on request.

---

## 2. Executive Function Support

### The Problem

Executive function challenges affect task initiation, planning, time awareness,
working memory, and emotional regulation. Large, ambiguous tasks trigger
paralysis. Without external structure, it is difficult to know what to do first,
how long things will take, or when to switch tasks.

### Key Patterns

**AI-Powered Task Breakdown**
- Automatically decompose large tasks into small, actionable steps.
- Each step should be completable in a short focused session (5-15 minutes).
- Present steps sequentially, not all at once.
- Allow re-ordering and skipping without penalty.

**Visual Progress Tracking**
- Color-coded progress bars showing completion state.
- Visual timelines that externalize the passage of time.
- "Breadcrumb" navigation showing where the user is in a multi-step process.
- Satisfying visual feedback when steps are completed (checkmarks, progress fills).

**Task Initiation Support**
- Reduce friction to starting: the first action should require minimal effort.
- "Start here" indicators that remove ambiguity about what to do next.
- Pre-populated templates and starter code to avoid blank-page paralysis.
- The AI should offer to begin tasks on behalf of the user, requiring only approval.

**Context Preservation**
- Allow pausing and resuming without losing progress or context.
- Automatic save-state so interrupted work can be picked up seamlessly.
- When returning to a task, provide a brief recap of where the user left off.
- Breadcrumbs and signposts identifying current task/subtask status.

**Time Awareness**
- Gentle time indicators (not stressful countdowns).
- Optional session timers with soft reminders.
- Avoid hard time limits except for critical operations.
- If timeouts are necessary, warn 20+ seconds in advance with simple extension.

### Framework Application

- The voice assistant should break down requests like "make a platformer" into
  ordered subtasks and present them one at a time.
- Every session should begin with a recap: "Last time you were working on X.
  You completed steps 1-3. Ready to continue with step 4?"
- Visual dashboard showing project completion at a glance with color coding.

---

## 3. Sensory Considerations

### The Problem

85% of neurodiverse individuals perceive colors more intensely than neurotypical
users. Bright, high-contrast, visually busy interfaces cause sensory overload.
Auto-playing media, unexpected sounds, and animations can cause distress.
WCAG does not adequately address excessive visual or auditory stimulation.

### Key Patterns

**Color Palette**
- Use muted, calming colors: soft blues, greens, warm grays, creams.
- Avoid pure white (#ffffff) backgrounds -- they are too bright and tiring.
  Use off-white or very light warm grays instead (e.g., #f5f5f0, #fafaf8).
- Avoid bright, bold, or clashing color combinations.
- Use color meaningfully (for status, categories) but never as the only indicator.
- Maintain sufficient contrast (1.5:1 minimum for control boundaries) without
  being harsh.

**Animation and Motion**
- No auto-playing animations or media.
- Respect `prefers-reduced-motion` system setting.
- All animations should be toggleable.
- Transitions should be smooth and predictable, not sudden or jarring.
- Use subtle, purposeful animations only (e.g., a gentle progress fill).

**Audio Design**
- No auto-playing audio.
- All sounds should be controllable (volume, mute, type).
- Audio feedback should be brief, pleasant, and informative -- not startling.
- Provide visual alternatives for all audio cues.
- Take inspiration from Slack's approach: consistent, low-stress notification
  tones designed after studying responses from users with anxiety and ADHD.

**Customization**
- Allow users to adjust: color scheme, font size, animation level, audio volume.
- Provide preset "sensory profiles" (e.g., "calm", "minimal", "standard").
- Remember preferences across sessions.
- Offer a "focus mode" that strips the interface to essential elements only.

### Framework Application

- Default theme should use a calming, low-stimulation palette.
- The Unity editor integration should offer a simplified, decluttered view.
- Voice feedback tones should be gentle, consistent, and brief.
- Provide a "zen mode" or "focus mode" that removes all non-essential UI.

---

## 4. Predictability for Autism

### The Problem

Autistic users often experience heightened anxiety around ambiguity,
unpredictable system behavior, and changes in routine. Interfaces that change
layout between updates, use inconsistent patterns, or employ figurative language
create confusion and stress.

### Key Patterns

**Consistent Layout and Navigation**
- Navigation elements must appear in the same position on every screen.
- Similar types of information should always use the same visual structure.
- Icons and controls must be consistent across all pages and states.
- Use standard layout conventions (search top-right, navigation left/top).

**Explicit State Communication**
- Always show clear system status (loading, processing, complete, error).
- Provide visual feedback for every action ("saved", "sent", "building").
- Never leave the user guessing what the system is doing.
- Use labeled mechanisms to control interruptions.

**Literal, Unambiguous Language**
- Avoid sarcasm, idioms, metaphors, and humor in UI text.
- Label all icons with text -- no "mystery meat" navigation.
- Use plain, literal language for all instructions and feedback.
- Avoid jargon; when technical terms are necessary, provide inline definitions.
- No double negatives or ambiguous phrasing.

**No Surprises**
- Warn before any destructive or irreversible action.
- Show summaries before submitting or executing important operations.
- Changes to the interface should be announced and explained.
- Provide undo for all non-destructive operations.
- Allow single-step return to previous process steps.

**Clear Expectations**
- Tell users what will happen before they take an action.
- Explain what each step of a process involves before starting.
- Provide clear error messages that explain what went wrong and what to do next.
- Use "before/after" previews for code changes or scene modifications.

### Framework Application

- The voice assistant should always confirm what it understood and what it will
  do before executing: "I'll create a player controller with jump mechanics.
  This will add a new script and a prefab. Proceed?"
- Error messages should be specific and actionable, never vague.
- UI layout must never change between sessions unexpectedly.
- The AI should never take irreversible actions without explicit confirmation.

---

## 5. ADHD Engagement

### The Problem

ADHD brains have lower dopamine activity in motivation and reward areas. Delayed
rewards lose their motivational power. Repetitive tasks without feedback cause
disengagement. The brain craves novelty but can be overwhelmed by chaos.

### Key Patterns

**Immediate Feedback Loops**
- Every action should produce visible feedback within milliseconds.
- Dopamine release occurs during reward anticipation, not receipt -- so
  progress indicators and "almost there" states are powerful motivators.
- Completion animations, checkmarks, and progress fills provide the micro-rewards
  the ADHD brain needs.
- A 2022 study in JMIR Serious Games found apps with game elements had 48%
  higher retention than non-gamified ones.

**Gamification (Purposeful, Not Gimmicky)**
- Use multiple reward types to prevent habituation: completion rewards, progress
  rewards, discovery rewards.
- Points, streaks, or badges for consistent work sessions.
- Visual "level up" moments when milestones are reached (e.g., first scene built,
  first playable build, first published game).
- Keep gamification subtle and optional -- it should feel encouraging, not
  condescending.

**Novelty Without Chaos**
- Vary the presentation of tasks to maintain interest (different UI for
  scripting vs. level design vs. testing).
- Occasional positive surprises ("You've been building for 30 minutes -- nice
  focus session!") but in predictable patterns.
- New feature discovery should feel rewarding, not overwhelming.
- Rotate tips and suggestions to keep the experience fresh.

**Short Reward Cycles**
- Break work into sessions with clear start/end points.
- Celebrate small wins -- completing a single feature, fixing a bug, adding an asset.
- Provide "daily recap" summaries showing what was accomplished.
- The shortest path from action to visible result should be prioritized.

**Friction Reduction for Task Initiation**
- Auto-save eliminates the friction of "remembering to save."
- One-click/one-command actions for common operations.
- Smart defaults that let users start immediately.
- The AI should offer to handle boilerplate and setup.

### Framework Application

- "Build and test" should be one command with immediate visual result in Unity.
- The voice assistant should acknowledge every completed step with brief,
  encouraging feedback.
- Progress tracking with visual milestones for the overall project.
- Optional "achievement" system for learning new framework features.
- Session summaries: "Today you created 2 scripts, set up the player controller,
  and fixed the collision bug."

---

## 6. Voice-First UX for Neurodiverse Users

### The Problem

Voice interaction can reduce cognitive load by eliminating the need to navigate
complex visual interfaces, but it can also create challenges: verbose responses
overwhelm ADHD users, ambiguous commands frustrate autistic users, and error
recovery in voice UIs is often poor.

### Key Patterns

**Brevity and Clarity**
- Responses must be concise. A person with ADHD can be overwhelmed by verbose,
  obligatory responses (ACM CUI@CHI 2023 workshop finding).
- Lead with the answer, then offer details on request.
- Use the Maxim of Quantity: provide exactly as much information as needed to
  advance the conversation, no more.
- Avoid filler phrases ("Well, actually...", "Let me think about that...").

**Patience and Forgiveness**
- Never time out on user input prematurely.
- Gracefully handle incomplete, revised, or restated commands.
- Allow users to correct or clarify without starting over.
- Do not penalize pauses, false starts, or changes of mind.

**Explicit Confirmation**
- Always confirm understanding before executing: "I'll do X. Sound good?"
- Announce state changes: "Building now... Done. No errors."
- For multi-step operations, provide brief status at each step.
- Never assume intent on ambiguous commands -- ask clarifying questions.

**Multimodal Redundancy**
- Pair voice output with visual feedback (text display, status indicators).
- Allow switching between voice and keyboard/mouse at any time.
- Visual confirmation of voice commands (show what was heard, what will happen).
- Multiple ways to complete every task: voice, keyboard shortcuts, GUI buttons.

**Error Recovery**
- Clear, specific error descriptions -- not "something went wrong."
- Suggest concrete next steps when errors occur.
- Offer to retry, undo, or try an alternative approach.
- Never blame the user for misunderstandings.

**Tone and Personality**
- Warm but professional -- not overly casual or robotic.
- Supportive without being patronizing.
- Consistent personality across all interactions.
- Avoid sarcasm, idioms, or ambiguous humor.

### Framework Application

- Voice commands should be natural language: "Add a jump mechanic to the player"
  not "invoke add-component PlayerJump --force 10."
- The assistant should respond in 1-2 sentences, with "want more details?"
  available as follow-up.
- Every voice command should produce a visual echo showing what was understood.
- Error responses should be: what happened, why, and what to do about it.
- The assistant should remember context within a session: "make it higher" after
  discussing jump force should adjust the jump force, not ask "make what higher?"

---

## 7. Existing Research & Standards

### Academic Research

**IDE Accessibility for ADHD (Lancaster University, 2024)**
- Think-aloud study with 9 computing students using VS Code.
- Found three themes: self-confidence, interaction, and learning.
- Students with ADHD struggled with visually noisy IDE interfaces.
- Low perceptual load mode (visually clear) improved task initiation speed
  and overall performance for mentally active programming tasks.
- Source: https://arxiv.org/html/2506.10598v1

**Perceptual Load and IDE Performance (2023)**
- ADHD participants solved coding and debugging tasks in high vs. low perceptual
  load IDE modes.
- For active coding tasks, response time and speed were better in low load mode.
- Confirms: reducing visual noise improves ADHD coding performance.
- Source: https://arxiv.org/abs/2302.06376

**ADHD Professional Programmers (2024)**
- Mixed methods study analyzing r/ADHD_Programmers and surveying 493 professionals.
- Identified common challenges: task switching, sustained focus, context loss.
- Source: https://kaianew.github.io/GetMeInTheGroove.pdf

**Inclusive CUIs for Adults with ADHD (ACM CUI@CHI 2023)**
- Workshop paper on designing conversational interfaces for ADHD users.
- Key finding: verbosity in CUI responses is a primary barrier.
- Source: https://cui.acm.org/workshops/CHI2023/pdfs/nordberg_Inclusive_Conversational_User_Interfaces_for_Adults_with_ADHD_Final.pdf

**Neurodiverse Programmers and Parsons Problems (ACM SIGCSE 2024)**
- Exploratory study on whether Parsons problems (code rearrangement exercises)
  are accessible for neurodiverse programmers.
- Source: https://dl.acm.org/doi/abs/10.1145/3626252.3630898

**Designing Assistive Technologies with Neurodivergent Users (Oxford, 2025)**
- Traditional HCI methods often fail to engage neurodivergent users in early
  design phases. Close involvement of end users is essential.
- Source: https://academic.oup.com/iwc/advance-article/doi/10.1093/iwc/iwaf037/8276143

### Standards and Guidelines

**W3C COGA Task Force -- "Making Content Usable"**
- Supplemental guidance beyond WCAG for cognitive and learning disabilities.
- 10 key objectives with design patterns under each:
  1. Help users understand the site (familiar patterns, personalization)
  2. Help users find what they need (3-click rule, functional site maps)
  3. Use clear and understandable content (plain language, short sentences)
  4. Help users avoid mistakes (undo, summaries before submission, error prevention)
  5. Help users focus (breadcrumbs, signposts, interrupt control)
  6. Minimize cognitive load (chunked content, common vocabulary)
  7. Provide clear structure (consistent headings, white space, standard layouts)
  8. Provide clear visual communication (obvious affordances, action feedback)
  9. Ensure time sufficiency (avoid timeouts, easy extensions)
  10. Prevent exploitation (clear charges, no dark patterns)
- Source: https://www.w3.org/TR/coga-usable/

**WCAG Guidelines Most Relevant to Neurodiversity**
- 1.3 Adaptable: Content presentable in simplified form without losing structure.
- 1.4 Distinguishable: Clear foreground/background separation.
- 2.2 Enough Time: Sufficient time to process content.
- 2.4 Navigable: Help users navigate and determine location.
- 3.1 Readable: Text content readable and understandable.
- 3.2 Predictable: Appear and operate in predictable ways.
- 3.3 Input Assistance: Help users avoid and correct mistakes.
- Source: https://www.w3.org/WAI/cognitive/

### Notable Existing Tools

- **Tiimo**: Visual planner with AI task breakdown and executive function support.
- **Super Productivity**: ADHD-focused task management with timeboxing.
- **Habitica**: Gamified task management turning to-dos into RPG quests.
- **Slack**: Refined notification design after studying ADHD/anxiety user responses.

---

## 8. Actionable Design Principles for This Framework

Synthesized from all research above, these are the concrete design principles
that should guide every aspect of the Unity Dev Framework.

### P1: Start Simple, Reveal on Demand
Default to the simplest possible interface. Hide advanced options behind
progressive disclosure. Never present more than 5-7 items at once.

### P2: One Thing at a Time
Present tasks sequentially. The AI breaks down requests into steps and presents
them one at a time. Each step has a clear, completable objective.

### P3: Immediate, Visible Feedback
Every action produces visible confirmation within 500ms. Build/test results
appear instantly. Voice commands echo visually. Progress updates continuously.

### P4: Never Surprise, Always Confirm
Confirm understanding before executing. Preview changes before applying.
Warn before destructive actions. Provide undo for everything possible.

### P5: Calm Sensory Defaults
Muted color palette (soft blues, greens, warm grays). No pure white backgrounds.
No auto-playing media. All animations optional and subtle. Gentle audio cues.

### P6: Externalize Memory and State
Show where the user is (breadcrumbs, progress indicators). Recap context on
return. Auto-save everything. Visual project status dashboard.

### P7: Reduce Friction to Zero
Smart defaults, pre-populated templates, one-command common operations.
The AI handles boilerplate. Starting should require minimal decisions.

### P8: Brief Voice, Deep on Demand
Voice responses are 1-2 sentences. Details available on follow-up request.
No filler or verbosity. Lead with the answer. Offer "want to know more?"

### P9: Consistent and Predictable
Same layout every time. Same interaction patterns across features. Same
vocabulary. Same confirmation flow. Predictability is safety.

### P10: Celebrate Progress
Acknowledge completed steps. Show daily summaries. Visual milestones for
project progress. Optional gamification that feels encouraging, not childish.

### P11: Patient and Forgiving
No timeouts on input. Graceful handling of corrections and changes of mind.
Specific, helpful error messages. Never blame the user.

### P12: Multimodal Always
Every feature accessible via voice, keyboard, and GUI. Visual confirmation
of voice input. Audio confirmation of visual actions (optional). User chooses
their preferred interaction mode at any time.

### P13: Customizable Sensory Experience
User-adjustable color scheme, font size, animation level, and audio.
Preset sensory profiles ("calm", "minimal", "standard"). Preferences persist
across sessions.

### P14: Literal and Clear
No sarcasm, idioms, or ambiguous language. All icons labeled with text.
Technical terms defined inline. Error messages explain what, why, and next steps.

### P15: Support, Don't Overwhelm
The AI is a collaborator, not a lecturer. It offers help, does not force it.
It provides options without demanding decisions. It handles complexity so
the user can focus on creativity.

---

## Sources

- [UI/UX for ADHD: Designing Interfaces That Actually Help Students](https://din-studio.com/ui-ux-for-adhd-designing-interfaces-that-actually-help-students/)
- [Software Accessibility for Users with Attention Deficit Disorder](https://www.carlociccarelli.com/post/software-accessibility-for-users-with-attention-deficit-disorder)
- [Inclusive UX/UI for Neurodivergent Users](https://medium.com/design-bootcamp/inclusive-ux-ui-for-neurodivergent-users-best-practices-and-challenges-488677ed2c6e)
- [Neurodiversity in UX: Inclusive Design Principles](https://www.aufaitux.com/blog/neuro-inclusive-ux-design/)
- [Designing for ADHD in UX - UXPA](https://uxpa.org/designing-for-adhd-in-ux/)
- [ADHD-Friendly Content Design](https://www.influencers-time.com/designing-adhd-friendly-content-key-neuroinclusive-principles/)
- [Designing UX for Neurodiverse Users](https://dool.agency/designing-ux-for-neurodiverse-users/)
- [Designing for Autistic People - Smart Interface Design Patterns](https://smart-interface-design-patterns.com/articles/design-autism/)
- [Designing for Autism in UX - UXPA](https://uxpa.org/designing-for-autism-in-ux/)
- [Designing for the 15%: Neurodivergent UX - PRINT Magazine](https://www.printmag.com/industry-perspectives/why-neurodivergent-ux-is-the-future/)
- [Designing for autistic people - UX Collective](https://uxdesign.cc/designing-for-autistic-people-overview-of-existing-research-d6f6dc20710e)
- [Gamified Task Management for ADHD](https://magictask.io/blog/gamified-task-management-adhd-focus-productivity/)
- [How Gamification in ADHD Apps Boosts Retention](https://imaginovation.net/blog/gamification-adhd-apps-user-retention/)
- [Gamification ADHD: Making Tasks Easier to Start - Tiimo](https://www.tiimoapp.com/resource-hub/gamification-adhd)
- [Unlocking Focus: Color Palettes for Neurodivergent Students](https://www.walturn.com/insights/unlocking-focus-how-color-palettes-can-help-neurodivergent-students-thrive)
- [DESIGNA11Y: Colors and Autism](https://www.design-a11y.com/colors-autism)
- [Sensory-Friendly UX for Neurodiverse Audiences - UX Magazine](https://uxmag.com/articles/designing-inclusive-and-sensory-friendly-ux-for-neurodiverse-audiences)
- [Website Design for Neurodiversity - Adchitects](https://adchitects.co/blog/design-for-neurodiversity)
- [Voice Interfaces, AI, and Accessibility](https://medium.com/@rounakbajoriastar/voice-interfaces-ai-and-accessibility-a-new-era-of-inclusive-design-bdae509c0318)
- [Inclusive CUIs for Adults with ADHD (ACM CUI@CHI 2023)](https://cui.acm.org/workshops/CHI2023/pdfs/nordberg_Inclusive_Conversational_User_Interfaces_for_Adults_with_ADHD_Final.pdf)
- [Accessible CUIs: Considerations - Open University](https://oro.open.ac.uk/69720/)
- [VUI Design Principles - Google Design](https://design.google/library/speaking-the-same-language-vui)
- [Accessible IDE Design for ADHD Students (2024)](https://arxiv.org/html/2506.10598v1)
- [Effect of Perceptual Load on IDE Performance with ADHD](https://arxiv.org/abs/2302.06376)
- [ADHD Professional Programmers Study](https://kaianew.github.io/GetMeInTheGroove.pdf)
- [Neurodiverse Programmers and Parsons Problems (ACM 2024)](https://dl.acm.org/doi/abs/10.1145/3626252.3630898)
- [Designing Assistive Technologies with Neurodivergent Users](https://academic.oup.com/iwc/advance-article/doi/10.1093/iwc/iwaf037/8276143)
- [W3C COGA: Making Content Usable](https://www.w3.org/TR/coga-usable/)
- [W3C Cognitive Accessibility at WAI](https://www.w3.org/WAI/cognitive/)
- [Progressive Disclosure - Nielsen Norman Group](https://www.nngroup.com/articles/progressive-disclosure/)
- [Progressive Disclosure - Interaction Design Foundation](https://www.interaction-design.org/literature/topics/progressive-disclosure)
- [Towards Inclusive Web Design Guidelines for ADHD (INTERACT 2025)](https://dl.acm.org/doi/10.1007/978-3-032-05008-3_59)
- [Differences Between Neurodivergent and Neurotypical Software Engineers](https://link.springer.com/chapter/10.1007/978-3-032-04207-1_5)
