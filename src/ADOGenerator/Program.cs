﻿using ADOGenerator;
using ADOGenerator.Models;
using ADOGenerator.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .Build();

Console.WriteLine("Welcome to Azure DevOps Demo Generator! This tool will help you generate a demo environment for Azure DevOps.");

string id = Guid.NewGuid().ToString().Split('-')[0];
while (true)
{

    id.AddMessage("Enter the template name from the list of templates below");
    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "TemplateSetting.json");

    if (!File.Exists(templatePath))
    {
        id.ErrorId().AddMessage("TemplateSettings.json file not found.");
        break;
    }

    var templateSettings = File.ReadAllText(templatePath);
    var json = JObject.Parse(templateSettings);
    var groupwiseTemplates = json["GroupwiseTemplates"];

    if (groupwiseTemplates == null)
    {
        id.ErrorId().AddMessage("No templates found.");
        break;
    }

    foreach (var group in groupwiseTemplates)
    {
        var groupName = group["Groups"]?.ToString();
        Console.WriteLine(groupName);

        var templates = group["Template"];
        if (templates != null)
        {
            foreach (var template in templates)
            {
                var templateName = template["Name"]?.ToString();
                Console.WriteLine($"  └─ {templateName}");
            }
        }
    }

    id.AddMessage("Enter the template name from the list of templates above:");
    var selectedTemplateName = Console.ReadLine();

    if (!TryGetTemplateDetails(groupwiseTemplates, selectedTemplateName, out var templateFolder, out var confirmedExtension))
    {
        id.AddMessage($"Template '{selectedTemplateName}' not found in the list.");
        id.AddMessage("Would you like to try again or exit? (type 'retry' to try again or 'exit' to quit):");
        var userChoice = Console.ReadLine();
        if (userChoice?.Equals("exit", StringComparison.OrdinalIgnoreCase) == true)
        {
            id.ErrorId().AddMessage("Exiting the application.");
            Environment.Exit(0);
        }
        continue;
    }

    id.AddMessage("Enter your Azure DevOps organization name:");
    var organizationName = Console.ReadLine();

    id.AddMessage("Enter your Azure DevOps personal access token:");
    var patToken = ReadSecret();

    id.AddMessage("Enter the new project name:");
    var projectName = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(organizationName) || string.IsNullOrWhiteSpace(patToken) || string.IsNullOrWhiteSpace(projectName))
    {
        id.ErrorId().AddMessage("Validation error: All inputs must be provided. Exiting..");
        Environment.Exit(1);
    }

    var project = new Project
    {
        id = id,
        accessToken = patToken,
        accountName = organizationName,
        ProjectName = projectName,
        TemplateName = selectedTemplateName,
        selectedTemplateFolder = templateFolder,
        SelectedTemplate = templateFolder,
        isExtensionNeeded = confirmedExtension,
        isAgreeTerms = confirmedExtension
    };

    CreateProjectEnvironment(project);
}

bool TryGetTemplateDetails(JToken groupwiseTemplates, string selectedTemplateName, out string templateFolder, out bool confirmedExtension)
{
    templateFolder = string.Empty;
    confirmedExtension = false;

    foreach (var group in groupwiseTemplates)
    {
        var templates = group["Template"];
        if (templates == null) continue;

        foreach (var template in templates)
        {
            if (template["Name"]?.ToString().Equals(selectedTemplateName, StringComparison.OrdinalIgnoreCase) != true) continue;

            templateFolder = template["TemplateFolder"]?.ToString();
            var templateFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", templateFolder);
            var extensionsFilePath = Path.Combine(templateFolderPath, "Extensions.json");

            if (!Directory.Exists(templateFolderPath))
            {
                id.ErrorId().AddMessage($"Template '{selectedTemplateName}' is not found.");
                return false;
            }

            id.AddMessage($"Template '{selectedTemplateName}' is present.");
            id.AddMessage("Are you sure want to create a new project with the selected template? (yes/no): press enter to confirm");
            var confirm = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(confirm) && !confirm.Equals("yes", StringComparison.OrdinalIgnoreCase) && !confirm.Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (File.Exists(extensionsFilePath))
            {
                var extensionsFile = File.ReadAllText(extensionsFilePath);
                var extensionjson = JObject.Parse(extensionsFile);
                var extensions = extensionjson["Extensions"];
                if (extensions != null)
                {
                    foreach (var extension in extensions)
                    {
                        var extensionName = extension["extensionName"]?.ToString();
                        var link = extension["link"]?.ToString();
                        var licenseLink = extension["License"]?.ToString();

                        var href = ExtractHref(link);
                        var licenseHref = ExtractHref(licenseLink);

                        Console.WriteLine($"Extension Name: {extensionName}");
                        Console.WriteLine($"Link: {href}");
                        Console.WriteLine($"License: {licenseHref}");
                        Console.WriteLine();
                    }

                    if (extensions.HasValues)
                    {
                        id.AddMessage("Do you want to proceed with this extension? (yes/no):");
                        var userConfirmation = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(userConfirmation) && (userConfirmation.Equals("yes", StringComparison.OrdinalIgnoreCase) || userConfirmation.Equals("y", StringComparison.OrdinalIgnoreCase)))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            id.AddMessage("Agreed for license? (yes/no):");
                            Console.ResetColor();
                            var licenseConfirmation = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(licenseConfirmation) && (licenseConfirmation.Equals("yes", StringComparison.OrdinalIgnoreCase) || licenseConfirmation.Equals("y", StringComparison.OrdinalIgnoreCase)))
                            {
                                confirmedExtension = true;
                                id.AddMessage("Confirmed Extension installation");
                            }
                        }
                        else
                        {
                            id.AddMessage("Extension installation is not confirmed.");
                        }
                    }
                }
            }

            return true;
        }
    }

    return false;
}

string ExtractHref(string link)
{
    var startIndex = link.IndexOf("href='") + 6;
    var endIndex = link.IndexOf("'", startIndex);
    return link.Substring(startIndex, endIndex - startIndex);
}

string ReadSecret()
{
    var secret = string.Empty;
    ConsoleKey key;
    do
    {
        var keyInfo = Console.ReadKey(intercept: true);
        key = keyInfo.Key;

        if (key == ConsoleKey.Backspace && secret.Length > 0)
        {
            Console.Write("\b \b");
            secret = secret[0..^1];
        }
        else if (!char.IsControl(keyInfo.KeyChar))
        {
            Console.Write("*");
            secret += keyInfo.KeyChar;
        }
    } while (key != ConsoleKey.Enter);
    Console.WriteLine();
    return secret;
}

void CreateProjectEnvironment(Project model)
{
    Console.WriteLine($"Creating project '{model.ProjectName}' in organization '{model.accountName}' using template from '{model.TemplateName}'...");
    var projectService = new ProjectService(configuration);
    var result = projectService.CreateProjectEnvironment(model);
    Console.WriteLine("Project created successfully.");
    Console.WriteLine("Do you want to create another project? (yes/no): press enter to confirm");
    var createAnotherProject = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(createAnotherProject) || createAnotherProject.Equals("yes", StringComparison.OrdinalIgnoreCase) || createAnotherProject.Equals("y", StringComparison.OrdinalIgnoreCase))
    {
        createAnotherProject = "yes";
    }
    else
    {
        createAnotherProject = "no";
    }
    Console.WriteLine();
    if (createAnotherProject.Equals("no", StringComparison.OrdinalIgnoreCase))
    {
        PrintErrorMessage("Exiting the application.");
        Environment.Exit(0);
    }


    Console.WriteLine("Enter the template name from the list of templates below");
    // read the TemplateSetting.json
    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "TemplateSetting.json");
    if (File.Exists(templatePath))
    {
        var templateSettings = File.ReadAllText(templatePath);
        var json = JObject.Parse(templateSettings);

        var groupwiseTemplates = json["GroupwiseTemplates"];
        if (groupwiseTemplates != null)
        {
            foreach (var group in groupwiseTemplates)
            {
                var groupName = group["Groups"]?.ToString();
                Console.WriteLine(groupName);

                var templates = group["Template"];
                if (templates != null)
                {
                    foreach (var template in templates)
                    {
                        var templateName = template["Name"]?.ToString();
                        Console.WriteLine($"  └─ {templateName}");
                    }
                }
            }
        }
        Console.WriteLine("Enter the template name from the list of templates above:");
        var selectedTemplateName = Console.ReadLine();

        bool templateFound = false;
        bool confirmedExtension = false;
        foreach (var group in groupwiseTemplates)
        {
            var templates = group["Template"];
            if (templates != null)
            {
                foreach (var template in templates)
                {
                    if (template["Name"]?.ToString().Equals(selectedTemplateName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var templateFolder = template["TemplateFolder"]?.ToString();
                        var templateFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", templateFolder);
                        string extensionsFilePath = Path.Combine(templateFolderPath, "Extensions.json");

                        if (Directory.Exists(templateFolderPath))
                        {
                            // get the actual template name from the TemplateSetting.json
                            selectedTemplateName = template["Name"]?.ToString();

                            Console.WriteLine($"Template '{selectedTemplateName}' is present.");

                            Console.WriteLine("Are you sure want to create a new project with the selected template? (yes/no): press enter to confirm");
                            var confirm = Console.ReadLine();
                            if (string.IsNullOrWhiteSpace(confirm) || confirm.Equals("yes", StringComparison.OrdinalIgnoreCase) || confirm.Equals("y", StringComparison.OrdinalIgnoreCase))
                            {
                                confirm = "yes";
                            }
                            else
                            {
                                confirm = "no";
                            }
                            if (confirm.Equals("yes", StringComparison.OrdinalIgnoreCase))
                            {
                                // check for the template folder for Extensions.json file, if exis, read the file and show
                                if (File.Exists(extensionsFilePath))
                                {
                                    var extensionsFile = File.ReadAllText(extensionsFilePath);
                                    var extensionjson = JObject.Parse(extensionsFile);
                                    var extensions = extensionjson["Extensions"];
                                    if (extensions != null)
                                    {
                                        foreach (var extension in extensions)
                                        {
                                            var extensionName = extension["extensionName"]?.ToString();
                                            var link = extension["link"]?.ToString();
                                            var licenseLink = extension["License"]?.ToString();

                                            // Extract href from the link
                                            var href = ExtractHref(link);
                                            var licenseHref = ExtractHref(licenseLink);

                                            Console.WriteLine($"Extension Name: {extensionName}");
                                            Console.WriteLine($"Link: {href}");
                                            Console.WriteLine($"License: {licenseHref}");
                                            Console.WriteLine();
                                        }
                                        if (extensions.HasValues)
                                        {
                                            Console.WriteLine("Do you want to proceed with this extension? (yes/no):");
                                            var userConfirmation = Console.ReadLine();
                                            if (string.IsNullOrWhiteSpace(userConfirmation) || userConfirmation.Equals("yes", StringComparison.OrdinalIgnoreCase) || userConfirmation.Equals("y", StringComparison.OrdinalIgnoreCase))
                                            {
                                                userConfirmation = "yes";
                                            }
                                            else
                                            {
                                                userConfirmation = "no";
                                            }
                                            if (userConfirmation?.Equals("yes", StringComparison.OrdinalIgnoreCase) == true)
                                            {
                                                Console.ForegroundColor = ConsoleColor.Yellow;
                                                Console.WriteLine("Agreed for license? (yes/no):");
                                                Console.ResetColor();
                                                var licenseConfirmation = Console.ReadLine();
                                                if (string.IsNullOrWhiteSpace(licenseConfirmation) || licenseConfirmation.Equals("yes", StringComparison.OrdinalIgnoreCase) || licenseConfirmation.Equals("y", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    licenseConfirmation = "yes";
                                                }
                                                else
                                                {
                                                    licenseConfirmation = "no";
                                                }
                                                if (licenseConfirmation?.Equals("yes", StringComparison.OrdinalIgnoreCase) == true)
                                                {
                                                    // Store the confirmation in a variable
                                                    confirmedExtension = true;
                                                    Console.WriteLine($"Confirmed Extension installation");
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("Extension installation is not confirmed.");
                                            }
                                        }
                                    }
                                }
                                // Collect additional inputs
                                Console.WriteLine("Enter your Azure DevOps organization name:");
                                var organizationName = Console.ReadLine();

                                Console.WriteLine("Enter your Azure DevOps personal access token:");
                                var patToken = ReadSecret();

                                string ReadSecret()
                                {
                                    var secret = string.Empty;
                                    ConsoleKey key;
                                    do
                                    {
                                        var keyInfo = Console.ReadKey(intercept: true);
                                        key = keyInfo.Key;

                                        if (key == ConsoleKey.Backspace && secret.Length > 0)
                                        {
                                            Console.Write("\b \b");
                                            secret = secret[0..^1];
                                        }
                                        else if (!char.IsControl(keyInfo.KeyChar))
                                        {
                                            Console.Write("*");
                                            secret += keyInfo.KeyChar;
                                        }
                                    } while (key != ConsoleKey.Enter);
                                    Console.WriteLine();
                                    return secret;
                                }

                                Console.WriteLine("Enter the new project name:");
                                var projectName = Console.ReadLine();

                                // Validate inputs
                                if (string.IsNullOrWhiteSpace(organizationName) || string.IsNullOrWhiteSpace(patToken) || string.IsNullOrWhiteSpace(projectName))
                                {
                                    Console.WriteLine("Validation error: All inputs must be provided. Exiting..");
                                    Environment.Exit(1);
                                }
                                // Pass the collected inputs to the CreateProjectEnvironment method                              
                                Project project = new Project()
                                {
                                    id = new Guid().ToString().Split('-')[0],
                                    accessToken = patToken,
                                    accountName = organizationName,
                                    ProjectName = projectName,
                                    TemplateName = selectedTemplateName,
                                    selectedTemplateFolder = templateFolder,
                                    SelectedTemplate = templateFolder,
                                    isExtensionNeeded = confirmedExtension,
                                    isAgreeTerms = confirmedExtension
                                };
                                CreateProjectEnvironment(project);
                            }
                        }
                        else
                        {
                            PrintErrorMessage($"Template '{selectedTemplateName}' is not found.");
                        }

                        templateFound = true;
                        break;
                    }
                }
            }

            if (templateFound)
            {
                break;
            }
        }

        if (!templateFound)
        {
            Console.WriteLine($"Template '{selectedTemplateName}' not found in the list.");
            Console.WriteLine("Would you like to try again or exit? (type 'retry' to try again or 'exit' to quit):");
            var userChoice = Console.ReadLine();
            if (userChoice?.Equals("exit", StringComparison.OrdinalIgnoreCase) == true)
            {
                Console.WriteLine("Exiting the application.");
                Environment.Exit(0);
            }
        }
    }
    else
    {
        PrintErrorMessage("TemplateSettings.json file not found.");
    }

    string ExtractHref(string link)
    {
        var startIndex = link.IndexOf("href='") + 6;
        var endIndex = link.IndexOf("'", startIndex);
        return link.Substring(startIndex, endIndex - startIndex);
    }

    void CreateProjectEnvironment(Project model)
    {
        // Implement the logic to create the project environment using the provided inputs
        Console.WriteLine($"Creating project '{model.ProjectName}' in organization '{model.accountName}' using template from '{model.TemplateName}'...");
        var projectService = new ProjectService(configuration);


        var result = projectService.CreateProjectEnvironment(model);
        // Add your implementation here

        Console.WriteLine("Project created successfully.");
        Console.WriteLine("Do you want to create another project? (yes/no): press enter to confirm");
        var createAnotherProject = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(createAnotherProject) || createAnotherProject.Equals("yes", StringComparison.OrdinalIgnoreCase) || createAnotherProject.Equals("y", StringComparison.OrdinalIgnoreCase))
        {
            createAnotherProject = "yes";
        }
        else
        {
            createAnotherProject = "no";
        }
        Console.WriteLine();
        if (createAnotherProject.Equals("no", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Exiting the application.");
            Environment.Exit(0);
        }
    }
}

void PrintErrorMessage(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: {message}");
    Console.ResetColor();
}
