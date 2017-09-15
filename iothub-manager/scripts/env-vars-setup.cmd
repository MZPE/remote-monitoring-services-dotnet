::  Prepare the environment variables used by the application.
::
::  For more information about finding IoT Hub settings, more information here:
::
::  * https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-create-through-portal#endpoints
::  * https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-getstarted
::

:: see: Shared access policies => key name => Connection string
SETX PCS_IOTHUB_CONNSTRING "..."

SETX PCS_UICONFIG_WEBSERVICE_URL "..."