# Dropoff

A simple text-hosting server and client to easily dump random files/text and fetch from either a commandline client or web page.

## Client
The client can be configured to connect to an Azure AD instance for authentication. This is useful if you put the Dropoff Server behind the same Azure AD.

### Requirements
1. [Dotnet 2.1 SDK](https://www.microsoft.com/net/download/windows)

### Setup
In the `Dropoff` folder:
Add an fill the `app.config` with the proper parameters:
- `"DropoffServer"`
- `"ida:AADInstance"`
- `"ida:Tenant"`
- `"ida:ClientId"`
- `"ida:ClientSecret"`

### Run
In the `Dropoff` folder:

`dotnet run -r 00000000000000000000000000000000`

#### Help
```bash
Dropoff Client v0.1.0

  -s, --server <server>    Dropoff server to communicate with (if 
                           different than one provided in app.config).
  -r, --retrieve <id>      Id of a file to retrieve from the Dropoff store.
  -h, --help               Display this help.
```

## Server

### Requirements
1. [Dotnet 2.1 SDK](https://www.microsoft.com/net/download/windows)

### Setup
In the `Dropoff` folder:
Add an fill the `appsettings.json` with the proper parameters:
- `"DROPOFF_STORE"`: Folder where Dropoff Server stores the incoming files. 

### Run
1. Clone the repository
3. `dotnet run --project Dropoff.Server\Dropoff.Server.csproj`

## Development

Run tests with: `dotnet test`

## License
MIT License Copyright (c) 2018 Devin Carr