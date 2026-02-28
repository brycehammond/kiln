using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Kiln.MCP.Editor
{
    public class ExplainErrorTool : ToolBase
    {
        public override string Name => "explain_error";
        public override string Description => "Explain a Unity error message in plain English";

        private static readonly List<(Regex pattern, string explanation, string fix)> ErrorPatterns = new List<(Regex, string, string)>
        {
            (new Regex(@"NullReferenceException", RegexOptions.IgnoreCase),
             "The code tried to use something that doesn't exist or hasn't been set up yet.",
             "Check that all variables are assigned in the Inspector or initialized in code. Look for GetComponent calls that might return null."),

            (new Regex(@"MissingComponentException", RegexOptions.IgnoreCase),
             "The code is trying to use a component that isn't attached to the GameObject.",
             "Add the missing component to the GameObject in the Inspector, or add a RequireComponent attribute to your script."),

            (new Regex(@"MissingReferenceException", RegexOptions.IgnoreCase),
             "The code is referencing an object that has been destroyed.",
             "Check if the object was destroyed elsewhere. Use null checks before accessing potentially destroyed objects."),

            (new Regex(@"Can't add component .* because .* already contains", RegexOptions.IgnoreCase),
             "You're trying to add a component that's already on this GameObject, and only one is allowed.",
             "Check if the component already exists before adding it. Use GetComponent to verify."),

            (new Regex(@"IndexOutOfRangeException", RegexOptions.IgnoreCase),
             "The code tried to access an item in a list or array using a number that's too big or too small.",
             "Check the length of your array/list before accessing elements. Remember that indices start at 0."),

            (new Regex(@"StackOverflowException", RegexOptions.IgnoreCase),
             "A function is calling itself over and over without stopping. This creates an infinite loop.",
             "Look for recursive function calls and make sure there's a condition that stops the recursion."),

            (new Regex(@"Shader error", RegexOptions.IgnoreCase),
             "There's a problem with a shader (the code that controls how things look on screen).",
             "Check the shader code for syntax errors. Make sure you're using a shader compatible with your render pipeline."),

            (new Regex(@"The type or namespace name .* could not be found", RegexOptions.IgnoreCase),
             "The code is using a name that C# doesn't recognize. It might be a missing 'using' statement or a typo.",
             "Add the correct 'using' statement at the top of the file, or check for typos in the type name."),

            (new Regex(@"Assets.*\.cs\(\d+,\d+\): error CS", RegexOptions.IgnoreCase),
             "There's a C# coding error in one of your scripts.",
             "Look at the file and line number in the error message. Common issues: missing semicolons, wrong brackets, typos."),

            (new Regex(@"SerializedObjectNotCreatableException", RegexOptions.IgnoreCase),
             "Unity can't create or save this object properly.",
             "Make sure your ScriptableObject has the CreateAssetMenu attribute, or check that your MonoBehaviour is on a valid GameObject."),

            (new Regex(@"Rigidbody2D.*3D|Rigidbody.*2D|Cannot.*2D.*3D", RegexOptions.IgnoreCase),
             "You're mixing 2D and 3D physics components. Unity keeps 2D and 3D physics separate.",
             "Use either all 2D components (Rigidbody2D, BoxCollider2D) or all 3D components (Rigidbody, BoxCollider) on the same object. Don't mix them."),

            (new Regex(@"Sprite.*null|SpriteRenderer.*no sprite", RegexOptions.IgnoreCase),
             "A SpriteRenderer doesn't have a sprite image assigned to it.",
             "Assign a sprite in the Inspector by dragging an image onto the Sprite field of the SpriteRenderer component."),

            (new Regex(@"Tilemap|TilemapRenderer", RegexOptions.IgnoreCase),
             "There's an issue with the Tilemap system used for grid-based 2D levels.",
             "Make sure you have a Grid parent object with a Tilemap child. Check that tiles are properly painted and the tilemap palette is set up."),

            (new Regex(@"Sorting Layer", RegexOptions.IgnoreCase),
             "There's an issue with sprite sorting layers, which control draw order in 2D games.",
             "Check that the sorting layer name exists in Project Settings > Tags and Layers. Verify the sorting order values.")
        };

        public override async Task<JObject> Execute(JObject parameters)
        {
            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                var errorMessage = parameters["errorMessage"]?.ToString() ?? "";
                var context = parameters["context"]?.ToString();

                foreach (var (pattern, explanation, fix) in ErrorPatterns)
                {
                    if (pattern.IsMatch(errorMessage))
                    {
                        var detail = $"Error: {errorMessage}\n\nWhat happened: {explanation}\n\nWhat to do: {fix}";
                        if (!string.IsNullOrEmpty(context))
                            detail += $"\n\nContext: {context}";

                        var spoken = $"{explanation} {fix}";
                        return Success(detail, spoken);
                    }
                }

                // Unknown error
                var unknownDetail = $"Error: {errorMessage}\n\nI don't recognize this specific error pattern.";
                if (!string.IsNullOrEmpty(context))
                    unknownDetail += $"\n\nContext: {context}";

                return Success(
                    unknownDetail,
                    "I'm not sure about this error. Try searching the Unity documentation or forums for the exact error message."
                );
            });
        }
    }
}
