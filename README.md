# TokenStore_FunctionBinding
~ Using bindings to link Azure Functions and Token Store ~ <br />
This github repo provides a dll for an Azure Function binding that accesses a specified Token Store. <br />
## ** Example of using this custom binding to create an Azure Function that lists files in a dropbox account **
### * SETUP *
1. Clone THIS (TokenStore_FunctionBinding) repo to a local directory 
	- Github Repo Link: https://github.com/amelhassan-msft/TokenStore_FunctionBinding.git

2. Create an Azure Function App in Visual Studios 
	- Notes: Visual Studio version 2019 was used 
	- Launch Visual Studios and create a new project using the Azure Functions template 
	- For your first function, select the "Http trigger" template, use or create a Storage Account, and set the Authorization level to "Anonymous" <br />
		<img src="/Images/FunctionApp/httptrigger.png" width="500">
	- In your HTTP Triggered function, paste the following code 
		- Source Code: https://github.com/amelhassan-msft/TokenStore_FunctionBinding/blob/master/dropbox_example.cs
		- Dependencies: Install nuget package for Dropbox.Api
	- In order for your project to contain a definition for this custom binding, you must add the dll from the TokenStore_FunctionBinding project as an assembly reference 
		- In your Azure Function App project in visual studios, under the "Solution Explorer" tab, right click on "Dependencies" and choose "Add reference" 
		- Navigate to the "Browse" tab on the left and click "Browse" on the bottom. 
		- Find the TokenStoreBindingProject dll (for the locally cloned  TokenStore_FunctionBinding Github repo ) and add it as a reference 
			- Sample path to dll: C:\Users\[user]\Documents\Github\TokenStore_FunctionBinding\TokenStoreBindingProject\TokenStoreBindingProject\bin\Debug\netstandard2.0\bin
	- If successful, adding the TokenStore Binding should no longer produce an error 

3. Deploying Azure Functions Visual Studio Project to the Azure Portal 
	- Under the "Build" tab in the visual studios interface, click "Publish [project name]"
	- Click Start and create a new app 
	- Choose a name for your app, a subscription, resource group, hosting plan, and azure storage (Note: these choices do not matter for the demo)
	- Hit "create". Your app will take a few minutes to deploy. If successful your output console should looking something like the following: 
	- Whenever you make a change to your local Azure Function, use the Publish interface to deploy your changes to your Azure Portal. 

4. Setting up Authentication Configurations for your Azure Function App
	- Navigate to https://ms.portal.azure.com
	- Set MSI ON 
		- Find your Azure Function App under the "Function App" tab 
		- Navigate to the "Platform features" tab and choose "Identity" under "Networking" 
		- Switch system assigned managed identity to "On" and save your changes. 
	- Connecting to the dropbox service 
		i. Follow the dropbox example outline here: https://github.com/Azure/azure-tokens/blob/master/docs/service-definition-reference.md
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
	- Select a subscription and resource group (choose the ones used when creating the new Azure Function and Visual Studios), create a Store Name and record for future use, select any location desired. 
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
- After setup is complete, publish your final version of your Azure Function app and run the function (either in the portal or by using the function url). If successful, your output should looking something like the following: 
<img src="/Images/output.png" width="500">

### *OTHER RESOURCES *
Other Resources 
- Guide on creating custom input and output bindings for Azure Functions 
	- https://github.com/Azure/azure-webjobs-sdk/wiki/Creating-custom-input-and-output-bindings
	- https://jan-v.nl/post/create-your-own-custom-bindings-with-azure-functions
2. Manually install or update Azure Functions binding extensions from the Azure portal 
	- https://docs.microsoft.com/en-us/azure/azure-functions/install-update-binding-extensions-manual
3. Microsoft graph bindings 
	- https://github.com/Azure/azure-functions-microsoftgraph-extension/
