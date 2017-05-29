# ZWave Serial Library

This library exposes a .NET API to interact with Serial-based Z-Wave Controllers. 
It may be used in any .NET project supporting the .NET Standard 1.5 and above. 
This class library is based off the Z-Wave Public Specification.

# MacOS/Linux Support

The library uses the [SerialPortStream](https://github.com/jcurl/SerialPortStream) library for serial IO. 
For MacOS and Linux support, the **libnserial** must be compiled. Follow the directions to compile the library. 

Note, for MacOS you encounter an error about framework. Add the following line to the Install section:

```
FRAMEWORK DESTINATION "${CMAKE_INSTALL_LIBDIR}"
```

Once compiled, copy the `dll/serialunix/bin/usr/local/lib/nserial.framework/nserial` file to `/usr/local/lib/libnserial.so.1`

For the ZStick on MacOs, it should be located on `/dev/cu.usbmodem411`.