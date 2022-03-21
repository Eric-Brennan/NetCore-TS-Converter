using NSwag;
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
    * ConvertTo-TypeScriptFromOpenAPI -SFP "C:\Users\<YOUR.USERNAME>\Desktop"
    */


    [Cmdlet(VerbsData.ConvertTo, "TypeScriptFromOpenAPI")]
    public class ConvertToTypeScriptFromOpenAPI : Cmdlet
    {
        [Alias("SFP, SolutionFolderPath")]
        [Parameter(Position = 0, Mandatory = true)]
        public string SolutionFolderPath { get; set; }

        protected override void ProcessRecord()
        {
            DirectoryInfo dir = new DirectoryInfo(SolutionFolderPath);
            
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
                UseTransformOptionsMethod = true
            };

            //Add base class from embedded resource
            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream($"APIConverter.ApiClientBase.ts"))
                using(StreamReader reader = new StreamReader(stream))
            {
                settings.TypeScriptGeneratorSettings.ExtensionCode = reader.ReadToEnd();
            }

            //Generate code
            WriteObject("Generating code...");

            var outputFilePath = SolutionFolderPath + $"\\api-types.ts";

            File.WriteAllText(outputFilePath, new TypeScriptClientGenerator(document.Result, settings).GenerateFile());

            WriteObject($"Client code generated at: { outputFilePath }");
        }
    }
}
