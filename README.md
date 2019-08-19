# TokenStore_FunctionBinding
~ Using bindings to link Azure Functions and Token Store ~ <br />
This github repo contains a visual studio project "TokenStoreBindingProject" that, once compiled, provides a dll for an Azure Function binding that accesses a specified Token Store to retrieve tokens, enabling interaction with external services. <br />
To use in your custom Azure Functions, simply add the project dll file (TokenStoreBindingProject.dll) as an assembly reference. 

## ** Sample Use Cases **
1. Updating a file in DropBox each time it is modified in Microsoft OneDrive 
2. Send yourself a text message using Twilio each time a file is uploaded to Google Drive

## ** Step by Step Example: Use a TokenStore input binding to create a timer-triggered Azure Function that lists files in a dropbox account **
### * SETUP *
1. Clone THIS repo (TokenStore_FunctionBinding) to a local directory, open the "TokenStoreBindingProject" in visual studio and build 
	- Github Repo Link: https://github.com/amelhassan-msft/TokenStore_FunctionBinding.git

2. Create an Azure Function App in Visual Studio 
	- Notes: Visual Studio version 2019 was used 
	- Launch Visual Studio and create a new project using the Azure Functions template 
	<img src="/Images/FunctionApp/newproj.png" width="500"> <br />
	- For your first function, select the "timer trigger" template, use or create a Storage Account, and set the Authorization level to "Anonymous" <br />
	- In your timer triggered function, paste the following code 
		- Source Code: https://github.com/amelhassan-msft/TokenStore_FunctionBinding/blob/master/AzureFunction_Examples/dropbox_timer_example.cs
		- Dependencies: Install nuget package for Dropbox.Api
	- In order for your project to contain a definition for this custom binding, you must add the dll from the TokenStore_FunctionBinding project as an assembly reference 
		- In your Azure Function App project in visual studio, under the "Solution Explorer" tab, right click on "Dependencies" and choose "Add reference" 
		<img  src="/Images/FunctionApp/addref.png" width="250"> <br />
		- Navigate to the "Browse" tab on the left and click "Browse" on the bottom. 
		- Find the TokenStoreBindingProject dll (for the locally cloned  TokenStore_FunctionBinding Github repo ) and add it as a reference 
			- Sample path to dll: C:\Users\[user]\Documents\Github\TokenStore_FunctionBinding\TokenStoreBindingProject\TokenStoreBindingProject\bin\Debug\netstandard2.0\bin\TokenStoreBindingProject.dll
			<img  src="/Images/FunctionApp/adddll.png" width="500"> <br />
	- If successful, adding the TokenStore Binding should no longer produce an error 

3. Deploying Azure Functions Visual Studio Project to the Azure Portal 
	- Under the "Build" tab in the visual studio interface, click "Publish [project name]"
	- Click Start and create a new app 
		<img src="/Images/Publishing/new.png" width="400"> <br />
	- Choose a name for your app, a subscription, resource group, hosting plan, and azure storage 
		<img src="/Images/Publishing/options.png" width="500"> <br />
	- Hit "create". Your app will take a few minutes to deploy. If successful your output console should output a publish success message. 
	- Whenever you make a change to your local Azure Function, use the Publish interface to deploy your changes to your Azure Portal. 

4. Setting up Authentication Configurations for your Azure Function App
	- Navigate to https://ms.portal.azure.com
	- Set MSI ON 
		- Find your Azure Function App under the "Function App" tab 
		- Navigate to the "Platform features" tab and choose "Identity" under "Networking" 
			<img src="/Images/Auth/path.png" width="500"> <br />
		- Switch system assigned managed identity to "On" and save your changes. 
			<img src="/Images/Auth/ID.png" width="300"> <br />
	- Connecting to the dropbox service 
		- Follow the dropbox example outline here: https://github.com/Azure/azure-tokens/blob/master/docs/service-definition-reference.md
	- OPTIONAL: If you want to work with Microsoft Graph Services: Set up Azure Active Directory (AAD) 
		- Find your Azure Function App under the "Function App" tab 
		- Navigate to the "Platform features" tab and choose "Authentication/Authorization" under "Networking" 
		- Switch App Service Authentication to "on" 
		- Set "Action to take when request is not authenticated" to "Allow Anonymous requests (no action)" 
		- Configure Azure Active Directory to Express and choose "Create New AD App". Click "Ok".   
		- Save your changes 
		- To set further permissions, go to the azure portal -> azure active directory -> app registrations-> and choose which APIs your app has permissions to 
5. Setup a TokenStore (currently in preview) 
	- Navigate to http://aka.ms/tokenstorems
	- Click "Create a resource" and choose Token Store
	- Select a subscription and resource group (choose the ones used when creating the new Azure Function and Visual Studio), create a Store Name and record for future use, select the desired location. 
	<img src="/Images/TokenStore/new.png" width="500"> <br />
	- Hit "Review + Create" and then "Create" to finalize your Token Store. 
	- Register apps that are using this Token Store under the "Access Policies" tab (Allows your app to access tokens within this Token Store) 
		- Example: Authorizing your Azure Function App to use your Token Store
			-  Under the access policies tab, select "add". Give the app you are registering a unique name and fill out the object ID and tenant ID (see instructions below on how to access these values). Check off Get, List, Create or Update, and delete. 
				- To access the object ID and tenant ID of your azure function app, navigate to https://ms.portal.azure.com
				- On the left, select "Azure Activity Directory" and within that interface, choose "App registrations" 
				- Click on your Azure Function App, and the object ID and Directory (tenant) ID will be listed.  
	- Add tokens under the "Services" tab 
		- Example: Adding a dropbox access token 
			- Under the "Services" tab, click "add". Name your service and choose the identity provider (in this case dropbox). Fill out "ClientId" and "ClientSecret" with the recorded app key and app secret from step 4. Click "Ok". 
			- Click the newly created service (i.e. "dropboxdemo") and click the  "Access Policies" tab. Click "add" to add a new token to your service. 

### * RUNNING THE EXAMPLE *
- After setup is complete, publish your final version of your Azure Function app and run the function in the portal. If successful, your output should looking something like the following: 
<img src="/Images/output_timer.png" width="700"> <br />

### * OTHER RESOURCES *
Other Resources 
1. Guide on creating custom input and output bindings for Azure Functions 
	- https://github.com/Azure/azure-webjobs-sdk/wiki/Creating-custom-input-and-output-bindings
	- https://jan-v.nl/post/create-your-own-custom-bindings-with-azure-functions
2. Manually install or update Azure Functions binding extensions from the Azure portal 
	- https://docs.microsoft.com/en-us/azure/azure-functions/install-update-binding-extensions-manual
3. Microsoft graph bindings 
	- https://github.com/Azure/azure-functions-microsoftgraph-extension/

## ** License **

This project is under the benevolent umbrella of the [.NET Foundation](http://www.dotnetfoundation.org/) and is licensed under [the MIT License](https://github.com/Azure/azure-webjobs-sdk/blob/master/LICENSE.txt)

## ** Contributing **

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.