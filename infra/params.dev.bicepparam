using './main.bicep'

param environment = 'dev'
param owner = 'platform-team'
param costCenter = 'demo'
param acrLoginServer = 'afcacrplatformebad4aa3.azurecr.io'
param containerImage = 'afcacrplatform.azurecr.io/afc-api:dev'
