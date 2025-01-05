# MSFS BouLED

Bouled is an application that synchronizes game controller LEDs with Microsoft Flight Simulator.

Thrusmater's HOTAS Warthog throttle is the only joystick currently supported.

The backlight is synchronized with the lighting event of the instrument panel lights (NAV light or panel light).
Intensity of the backlight is set according to panel light intensity (if available otherwise it is set to low intensity).

LEDs 1 to 4 light up according to flaps deployment.

The 5th LED light is up when the landing gear is down. The 5th LED blink when gear get up or down.

Once launched, the application resides in the notification area.

#### References
- [Communication with USB Devices using HID Protocol](https://www.codeproject.com/Articles/1244702/How-to-Communicate-with-its-USB-Devices-using-HID)
