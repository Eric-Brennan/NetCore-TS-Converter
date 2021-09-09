using NSwag;
using NSwag.CodeGeneration.TypeScript;
using System;
using System.IO;
using System.Management.Automation;

namespace VUETopia.PowerShell
{
    /* 
     * https://docs.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-standard-library-binary-module?view=powershell-7 
     *
     * Set-Location "C:\TFS 2018\VUETopia.PowerShell\VUETopia.PowerShell.Main\VUETopia.PowerShell"
     * Import-Module .\bin\Debug\netstandard2.0\VUETopia.PowerShell.dll (location of powershell must be root of project)
     * Get-Command -Module VUETopia.PowerShell
     * ConvertTo-TypeScriptFromOpenAPI -SFP "C:\TFS 2018\VUETopia.Broker\VUETopia.Broker"
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

            WriteObject("Checking directory....");

            //Check directory exists
            if (!dir.Exists)
            {
                WriteObject("The folder specified doesn't exist, please try again.");
                return;
            }

            //Find API directory
            var apiDir = dir.GetDirectories("*.API*");

            //Check folder exists with the name ".API" relies on projects being named in this convention
            if (apiDir == null || apiDir.Length == 0)
            {
                WriteObject("Could not find a folder with the name '.API' in it, please try again. The solution directory must contain a ASP.NET Core Web API with this naming convention.");
                return;
            }

            //Find UI directory (same name as API directory but without the 'API')
            var uiDir = new DirectoryInfo($"{ apiDir[0].Parent.FullName }/{ apiDir[0].Name.Replace(".API", "") }/src/api");

            //Check folder exists with the same name as API directory but without the ".API" in the name
            if (!uiDir.Exists)
            {
                WriteObject($"Could not find folder { uiDir.FullName }.");
                return;
            }

            //Find swagger file
            var swaggerFile = apiDir[0].GetFiles("swagger.json");

            if (swaggerFile == null || swaggerFile.Length == 0)
            {
                WriteObject($"Could not find a swagger.json file in { apiDir[0].FullName }, please try again.");
                return;
            }

            //Read swagger file
            WriteObject("Reading swagger file...");

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
            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream($"{ this.GetType().Namespace }.ApiClientBase.ts"))
                using(StreamReader reader = new StreamReader(stream))
            {
                settings.TypeScriptGeneratorSettings.ExtensionCode = reader.ReadToEnd();
            }

            //Generate code
            WriteObject("Generating code...");

            var outputFilePath = $"{ uiDir.FullName }\\api-client.ts";

            File.WriteAllText(outputFilePath, new TypeScriptClientGenerator(document.Result, settings).GenerateFile());

            WriteObject($"Client code generated at: { outputFilePath }");
        }
    }
}
