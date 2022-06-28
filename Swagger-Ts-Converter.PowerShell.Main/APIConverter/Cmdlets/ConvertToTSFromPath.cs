using NSwag;
using NSwag.CodeGeneration.OperationNameGenerators;
using NSwag.CodeGeneration.TypeScript;
using System;
using System.IO;
using System.Management.Automation;

namespace APIConverter
{
    /* 
    * https://docs.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-standard-library-binary-module?view=powershell-7 
    *
    * Set-Location "C:\Repos\Swagger-Ts-Converter\Swagger-Ts-Converter.PowerShell.Main\APIConverter"
    * Import-Module .\bin\Debug\netstandard2.0\APIConverter.dll
    * Get-Command -Module APIConverter
    * ConvertTo-TSFromPath -Path "C:\Users\<YOUR.USERNAME>\Desktop"
    */


    [Cmdlet(VerbsData.ConvertTo, "TSFromPath")]
    public class ConvertToTSFromPath : Cmdlet
    {
        [Alias("Path, Path")]
        [Parameter(Position = 0, Mandatory = true)]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            DirectoryInfo dir = new DirectoryInfo(Path);
            
            //Check directory exists
            if (!dir.Exists)
            {
                WriteObject("The folder specified doesn't exist, please try again.");
                return;
            }

            //Find swagger file
            var swaggerFile = dir.GetFiles("swagger.json");

            if (swaggerFile == null || swaggerFile.Length == 0)
            {
                WriteObject($"Could not find a swagger.json file, please try again.");
                return;
            }

            //Read swagger file
            var document = OpenApiDocument.FromFileAsync(swaggerFile[0].FullName);

            document.Wait();

            //Typescript settings for base authorisation class
            var settings = new TypeScriptClientGeneratorSettings
            {
                ClientBaseClass = "ApiClientBase",
                ConfigurationClass = "IConfig",
                OperationNameGenerator = new MultipleClientsFromFirstTagAndOperationIdGenerator()
            };

            //Add base class from embedded resource
            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream($"APIConverter.ApiClientBase.ts"))
                using(StreamReader reader = new StreamReader(stream))
            {
                settings.TypeScriptGeneratorSettings.ExtensionCode = reader.ReadToEnd();
            }

            //Generate code
            WriteObject("Generating code...");

            var outputFilePath = Path + $"\\api-types.ts";

            File.WriteAllText(outputFilePath, new TypeScriptClientGenerator(document.Result, settings).GenerateFile());

            WriteObject($"Client code generated at: { outputFilePath }");
        }
    }
}
