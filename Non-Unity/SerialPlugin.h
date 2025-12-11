// SerialPlugin.h
// Header file for Unity Serial Plugin
// Cross-platform serial communication interface

#ifndef SERIAL_PLUGIN_H
#define SERIAL_PLUGIN_H

#ifdef _WIN32
    #define EXPORT extern "C" __declspec(dllexport)
#else
    #define EXPORT extern "C"
#endif

// Opens a serial port with specified parameters
// Returns 1 on success, 0 on failure
EXPORT int SerialOpen(const char* portName, int baudRate, bool debug);

// Closes the currently open serial port
EXPORT void SerialClose();

// Changes the baud rate of the open serial port
// Returns 1 on success, 0 on failure
EXPORT int SerialSetBaud(int baudRate);

// Writes two bytes (position and speed) to the serial port
// Returns number of bytes written
EXPORT int SerialWrite(unsigned char position, unsigned char speed);

// Reads two bytes (position and speed) from the serial port
// Returns number of bytes read
EXPORT int SerialRead(unsigned char* position, unsigned char* speed);

// Internal functions for raw read/write operations
int SerialWriteInternal(const unsigned char* data, int length);
EXPORT int SerialReadInternal(unsigned char* buffer, int bufferSize);

#endif // SERIAL_PLUGIN_H