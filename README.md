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
	- For your first function, select the "Http trigger" template, use or create a Storage Account, and set the Authorization level to "Anonymous" 
		<img src="/Images/FunctionApp/httptrigger.png" width="60">
	- To your HTTP triggered function, add a TokenStore Binding following the format below
	- In order for your project to contain a definition for this custom binding, you must add the dll from the TokenStore_FunctionBinding project as an assembly reference 
		- In your Azure Function App project in visual studios, under the "Solution Explorer" tab, right click on "Dependencies" and choose "Add reference" 
		- Navigate to the "Browse" tab on the left and click "Browse" on the bottom. 
		- Find the TokenStoreBindingProject dll (for the locally cloned  TokenStore_FunctionBinding Github repo ) and add it as a reference 
			- Sample path to dll: C:\Users\[user]\Documents\Github\TokenStore_FunctionBinding\TokenStoreBindingProject\TokenStoreBindingProject\bin\Debug\netstandard2.0\bin
	- If successful, adding the TokenStore Binding should no longer produce an error 
### * RUNNING THE EXAMPLE *
