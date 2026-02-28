using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Kiln.MCP.Editor
{
    public class AddPackageTool : ToolBase
    {
        public override string Name => "add_package";
        public override string Description => "Install a Unity Package Manager package";

        private const int TimeoutMs = 60_000;
        private const int PollIntervalMs = 500;

        public override async Task<JObject> Execute(JObject parameters)
        {
            var identifier = parameters["identifier"]?.ToString();

            if (string.IsNullOrEmpty(identifier))
                return Failure("identifier is required.", "I need a package identifier to install.");

            try
            {
                // Client.Add must be called on the main thread
                var request = await MainThreadDispatcher.RunOnMainThread(() => Client.Add(identifier));

                // Poll for completion (IsCompleted is safe to read from any thread)
                var elapsed = 0;
                while (!request.IsCompleted && elapsed < TimeoutMs)
                {
                    await Task.Delay(PollIntervalMs);
                    elapsed += PollIntervalMs;
                }

                if (!request.IsCompleted)
                {
                    return Failure(
                        $"Package install timed out after {TimeoutMs / 1000}s for '{identifier}'.",
                        $"Installing {identifier} took too long. It might still be installing in the background."
                    );
                }

                if (request.Status == StatusCode.Failure)
                {
                    return Failure(
                        $"Failed to install '{identifier}': {request.Error?.message ?? "unknown error"}",
                        $"Could not install {identifier}. {request.Error?.message ?? ""}"
                    );
                }

                // Read result on main thread to be safe
                return await MainThreadDispatcher.RunOnMainThread(() =>
                {
                    var info = request.Result;
                    var detail = $"Installed package '{info.displayName}' ({info.packageId}) version {info.version}";
                    var spoken = $"Installed {info.displayName} version {info.version}.";
                    return Success(detail, spoken);
                });
            }
            catch (Exception ex)
            {
                return Failure(
                    $"Failed to install package: {ex.Message}",
                    $"Something went wrong installing the package. {ex.Message}"
                );
            }
        }
    }
}
