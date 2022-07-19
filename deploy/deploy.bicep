param name string = resourceGroup().name
param location string = 'canadaeast'
var storageAccountSuffix = 'storageyay1234'
var storageAccountName = '${toLower(replace(name, '-', ''))}${storageAccountSuffix}'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: '${name}-logs'
  location: location
  tags: {}
  properties: {
    features: {}
    sku: { name: 'Free' }
  }
}

resource appInsights 'microsoft.insights/components@2020-02-02-preview' = {
  kind: 'other'
  name: '${name}-insights'
  location: location
  tags: {}
  properties: {
    Application_Type: 'web',
    WorkspaceResourceId: logAnalytics.id
  }
  dependsOn: [logAnalytics]
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2018-07-01' = {
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  name: storageAccountName
  location: location
  tags: {}
  properties: {
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        file: {
          enabled: true
        }
        blob: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    accessTier: 'Hot'
  }
  dependsOn: []
}

resource hostingPlan 'Microsoft.Web/serverfarms@2018-02-01' = {
  name: name
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  kind: 'functionapp,linux'
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2018-11-01' = {
  name: name
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      use32BitWorkerProcess: false
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
    }
  }
  dependsOn: [
    storageAccount
    appInsights
  ]
}

resource functionAppAppSettings 'Microsoft.Web/sites/config@2018-11-01' = {
  name: '${functionApp.name}/appsettings'
  properties: {
    FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
    FUNCTIONS_EXTENSION_VERSION: '~4'
    AzureWebJobsStorage: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${listKeys(storageAccount.id, '2015-05-01-preview').key1}'
    APPINSIGHTS_INSTRUMENTATIONKEY: reference(appInsights.id, '2015-05-01').InstrumentationKey
    subscriptionId: subscription().subscriptionId
  }
}
