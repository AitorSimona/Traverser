# Sensor support

Sensors are [`InputDevices`](Devices.md) that measure environmental characteristics of the device that the content is running on. Unity currently supports sensors on iOS and Android. Android supports a wider range of sensors than iOS.

Unlike other devices, sensors are disabled by default. To enable a sensor, call [`InputSystem.EnableDevice()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_EnableDevice_UnityEngine_InputSystem_InputDevice_)).

```
InputSystem.EnableDevice(Gyroscope.current);
```

To disable a sensor, call [`InputSystem.DisableDevice()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_DisableDevice_UnityEngine_InputSystem_InputDevice_).

```
InputSystem.DisableDevice(Gyroscope.current);
```

To check whether a sensor is currently enabled, use [`InputDevice.enabled`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_enabled).

```
if (Gyroscope.current.enabled)
    Debug.Log("Gyroscope is enabled");
```

Each sensor Device implements a single Control which represents the data read by the sensor. The following sensors are available:

|Device|Android|iOS|Control|Type|
|------|-------|---|-------|----|
|[`Accelerometer`](#accelerometer)|Yes|Yes|[`acceleration`](../api/UnityEngine.InputSystem.Accelerometer.html#UnityEngine_InputSystem_Accelerometer_acceleration)|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|[`Gyroscope`](#gyroscope)|Yes|Yes|[`angularVelocity`](../api/UnityEngine.InputSystem.Gyroscope.html#UnityEngine_InputSystem_Gyroscope_angularVelocity)|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|[`GravitySensor`](#gravitysensor)|Yes|Yes|[`gravity`](../api/UnityEngine.InputSystem.GravitySensor.html#UnityEngine_InputSystem_GravitySensor_gravity)|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|[`AttitudeSensor`](#attitudesensor)|Yes|Yes|[`attitude`](../api/UnityEngine.InputSystem.AttitudeSensor.html#properties)|[`QuaternionControl`](../api/UnityEngine.InputSystem.Controls.QuaternionControl.html)|
|[`LinearAccelerationSensor`](#linearaccelerationsensor)|Yes|Yes|[`acceleration`](../api/UnityEngine.InputSystem.LinearAccelerationSensor.html#UnityEngine_InputSystem_LinearAccelerationSensor_acceleration)|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|[`MagneticFieldSensor`](#magneticfieldsensor)|Yes|No|[`magneticField`](../api/UnityEngine.InputSystem.MagneticFieldSensor.html#UnityEngine_InputSystem_MagneticFieldSensor_magneticField)|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|[`LightSensor`](#lightsensor)|Yes|No|[`lightLevel`](../api/UnityEngine.InputSystem.LightSensor.html#UnityEngine_InputSystem_LightSensor_lightLevel)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|[`PressureSensor`](#pressuresensor)|Yes|No|[`atmosphericPressure`](../api/UnityEngine.InputSystem.PressureSensor.html#UnityEngine_InputSystem_PressureSensor_atmosphericPressure)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|[`ProximitySensor`](#proximitysensor)|Yes|No|[`distance`](../api/UnityEngine.InputSystem.ProximitySensor.html#UnityEngine_InputSystem_ProximitySensor_distance)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|[`HumiditySensor`](#humiditysensor)|Yes|No|[`relativeHumidity`](../api/UnityEngine.InputSystem.HumiditySensor.html#UnityEngine_InputSystem_HumiditySensor_relativeHumidity)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|[`AmbientTemperatureSensor`](#ambienttemperaturesensor)|Yes|No|[`ambientTemperature`](../api/UnityEngine.InputSystem.AmbientTemperatureSensor.html#UnityEngine_InputSystem_AmbientTemperatureSensor_ambientTemperature)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|[`StepCounter`](#stepcounter)|Yes|No|[`stepCounter`](../api/UnityEngine.InputSystem.StepCounter.html#UnityEngine_InputSystem_StepCounter_stepCounter)|[`IntegerControl`](../api/UnityEngine.InputSystem.Controls.IntegerControl.html)|

## Sampling frequency

Sensors sample continuously at a set interval. You can set or query the sampling frequency for each sensor using the [`samplingFrequency`](../api/UnityEngine.InputSystem.Sensor.html#UnityEngine_InputSystem_Sensor_samplingFrequency) property. The frequency is expressed in Hertz (number of samples per second).

```
// Get sampling frequency of gyro.
var frequency = Gyroscope.current.samplingFrequency;

// Set sampling frequency of gyro to sample 16 times per second.
Gyroscope.current.samplingFrequency = 16;
```

## <a name="accelerometer"></a>[`Accelerometer`](../api/UnityEngine.InputSystem.Accelerometer.html)

Use the accelerometer to measure the acceleration of a device. This is useful to control content by moving a device around. It reports the acceleration measured on a device both due to moving the device around, and due to gravity pulling the device down. You can use `GravitySensor` and `LinearAccelerationSensor` to get separate values for these. Values are affected by the [__Compensate For Screen Orientation__](Settings.md#compensate-for-screen-orientation) setting.

## <a name="gyroscope"></a>[`Gyroscope`](../api/UnityEngine.InputSystem.Gyroscope.html)

Use the gyroscope to measure the angular velocity of a device. This is useful to control content by rotating a device. Values are affected by the [__Compensate For Screen Orientation__](Settings.md#compensate-for-screen-orientation) setting.

## <a name="gravitysensor"></a>[`GravitySensor`](../api/UnityEngine.InputSystem.GravitySensor.html)

Use the gravity sensor to determine the direction of the gravity vector relative to a device. This is useful to control content by device orientation. This is usually derived from a hardware `Accelerometer`, by subtracting the effect of linear acceleration (see `LinearAccelerationSensor`). Values are affected by the [__Compensate For Screen Orientation__](Settings.md#compensate-for-screen-orientation) setting.

## <a name="attitudesensor"></a>[`AttitudeSensor`](../api/UnityEngine.InputSystem.AttitudeSensor.html)

Use the attitude sensor to determine the orientation of a device. This is useful to control content by rotating a device. Values are affected by the [__Compensate For Screen Orientation__](Settings.md#compensate-for-screen-orientation) setting.

## <a name="linearaccelerationsensor"></a>[`LinearAccelerationSensor`](../api/UnityEngine.InputSystem.LinearAccelerationSensor.html)

Use the accelerometer to measure the acceleration of a device. This is useful to control content by moving a device around. Linear acceleration is the acceleration of a device unaffected by gravity. This is usually derived from a hardware `Accelerometer`, by subtracting the effect of gravity (see `GravitySensor`). Values are affected by the [__Compensate For Screen Orientation__](Settings.md#compensate-for-screen-orientation) setting.

## <a name="magneticfieldsensor"></a>[`MagneticFieldSensor`](../api/UnityEngine.InputSystem.MagneticFieldSensor.html)

This Input Device represents the magnetic field that affects the device which is running the content. Values are in micro-Tesla (μT) and measure the ambient magnetic field in the X, Y, and Z axis.

## <a name="lightsensor"></a>[`LightSensor`](../api/UnityEngine.InputSystem.LightSensor.html)

This Input Device represents the ambient light measured by the device which is running the content. Value is in SI lux units.

## <a name="pressuresensor"></a>[`PressureSensor`](../api/UnityEngine.InputSystem.PressureSensor.html)

This Input Device represents the atmospheric pressure measured by the device which is running the content. Value is in in hPa (millibar).

## <a name="proximitysensor"></a>[`ProximitySensor`](../api/UnityEngine.InputSystem.ProximitySensor.html)

This Input Device measures how close the device which is running the content is to the user. Phones typically use the proximity sensor to determine if the user is holding the phone to their ear or not. Values represent distance measured in centimeters.

## <a name="humiditysensor"></a>[`HumiditySensor`](../api/UnityEngine.InputSystem.HumiditySensor.html)

This Input Device represents the ambient air humidity measured by the device which is running the content. Values represent the relative ambient air humidity in percent.

## <a name="ambienttemperaturesensor"></a>[`AmbientTemperatureSensor`](../api/UnityEngine.InputSystem.AmbientTemperatureSensor.html)

This Input Device represents the ambient air temperature measured by the device which is running the content. Values represent temperature in Celsius degrees.

## <a name="stepcounter"></a>[`StepCounter`](../api/UnityEngine.InputSystem.StepCounter.html)

This Input Device represents the user's footstep count as measured by the device which is running the content.
