using NSwag;
using NSwag.CodeGeneration.TypeScript;
using System;
using System.IO;
using System.Management.Automation;
using System.Net;
using System.Net.Sockets;

namespace APIConverter
{
    /* 
     * https://docs.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-standard-library-binary-module?view=powershell-7 
     *
     * Set-Location "cd C:\Repos\Swagger-Ts-Converter\Swagger-Ts-Converter.PowerShell.Main\APIConverter"
     * Import-Module .\bin\Debug\netstandard2.0\APIConverter.dll
     * Get-Command -Module APIConverter
     * ConvertTo-TS_LocalHost -Port 55031
     */

    // Generates a typescript file which contains all types from the api
    // Allows close coupling of api and ui and speeds up dev process by generating complex types
    [Cmdlet(VerbsData.ConvertTo, "TS_LocalHost")]
    public class ConvertToTS_LocalHost : Cmdlet
    {
        [Alias("Port, Port")]
        [Parameter(Position = 0, Mandatory = true)]
        public int Port { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    // Get the swagger doc as string from localhost api
                    var json = wc.DownloadString("http://localhost:" + Port + "/swagger/v1/swagger.json");

                    // Create OpenAPI document from swagger string
                    var document = OpenApiDocument.FromJsonAsync(json);

                    document.Wait();

                    //Typescript settings for base authorisation class
                    var settings = new TypeScriptClientGeneratorSettings
                    {
                        ClientBaseClass = "ApiClientBase",
                        ConfigurationClass = "IConfig",
                        UseTransformOptionsMethod = true,
                        GenerateClientClasses = true
                    };

                    //Add base class from embedded resource
                    using (Stream strm = this.GetType().Assembly.GetManifestResourceStream($"APIConverter.ApiClientBase.ts"))
                    using (StreamReader reader = new StreamReader(strm))
                    {
                        settings.TypeScriptGeneratorSettings.ExtensionCode = reader.ReadToEnd();
                    }

                    //Generate code
                    WriteObject("Generating code...");

                    //Outout to C drive
                    var outputFilePath = $"C:\\api-types.ts";

                    File.WriteAllText(outputFilePath, new TypeScriptClientGenerator(document.Result, settings).GenerateFile());

                    string currentContent = File.ReadAllText(outputFilePath);
                    
                    File.WriteAllText(outputFilePath, "//@ts-nocheck \n" + currentContent);

                    WriteObject($"Client code generated at: { outputFilePath }");
                }
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }
    }
}
