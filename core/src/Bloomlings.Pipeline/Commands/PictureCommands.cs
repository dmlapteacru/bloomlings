using System.CommandLine;
using System.Linq;
using Bloomlings.Pipeline.Pictures;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Pipeline.Commands
{
    /// <summary><c>pictures import</c> and <c>pictures validate</c> (R7, T073, T074).</summary>
    public static class PictureCommands
    {
        public static Command Create()
        {
            var pictures = new Command("pictures", "Picture library: import and validate.");

            var import = new Command("import", "Convert indexed PNGs or .grid.txt files and sidecars into base-picture.v1 JSON with structure metrics.");
            Option<string> src = Cli.Path("--src", "content/pictures/src", "Source folder with <id>.meta.json and <id>.png|.grid.txt.");
            Option<string> outDir = Cli.Path("--out", "content/pictures/lib", "Library folder to write.");
            import.Options.Add(src);
            import.Options.Add(outDir);
            import.SetAction(parse => Cli.Run(parse, report =>
            {
                var results = PictureImporter.ImportFolder(parse.GetValue(src)!, parse.GetValue(outDir)!);
                var items = new JArray();
                foreach (ImportedPicture result in results)
                {
                    items.Add(new JObject { ["id"] = result.Id, ["ok"] = result.Error == null, ["error"] = result.Error });
                    if (result.Error != null)
                    {
                        Cli.Say(parse, $"  {result.Id}: {result.Error}");
                    }
                }

                int failed = results.Count(r => r.Error != null);
                report["imported"] = results.Count - failed;
                report["failed"] = failed;
                report["pictures"] = items;
                Cli.Say(parse, $"pictures import: {results.Count - failed} imported, {failed} failed.");
                return failed == 0 ? ExitCodes.Success : ExitCodes.ValidationFailed;
            }));

            var validate = new Command("validate", "Check pictures against the schema, size limits, roles, licence and review status.");
            Option<string> lib = Cli.Path("--lib", "content/pictures/lib", "Library folder.");
            validate.Options.Add(lib);
            validate.SetAction(parse => Cli.Run(parse, report =>
            {
                var reports = PictureValidator.ValidateFolder(parse.GetValue(lib)!);
                var items = new JArray();
                foreach (PictureReport picture in reports)
                {
                    items.Add(new JObject
                    {
                        ["file"] = System.IO.Path.GetFileName(picture.File),
                        ["usable"] = picture.Usable,
                        ["errors"] = new JArray(picture.Errors.ToArray()),
                        ["warnings"] = new JArray(picture.Warnings.ToArray()),
                    });
                    foreach (string error in picture.Errors)
                    {
                        Cli.Say(parse, $"  error {System.IO.Path.GetFileName(picture.File)}: {error}");
                    }
                }

                int errors = reports.Count(r => r.Errors.Count > 0);
                report["pictures"] = items;
                report["usable"] = reports.Count(r => r.Usable);
                report["withErrors"] = errors;
                Cli.Say(parse, $"pictures validate: {reports.Count} pictures, {reports.Count(r => r.Usable)} usable, {errors} with errors.");
                return errors == 0 ? ExitCodes.Success : ExitCodes.ValidationFailed;
            }));

            pictures.Subcommands.Add(import);
            pictures.Subcommands.Add(validate);
            return pictures;
        }
    }
}
