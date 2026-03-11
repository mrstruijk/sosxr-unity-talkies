// SerialPlugin.h
// Header file for Unity Serial Plugin
// Works with both Windows and macOS implementations

#ifndef SERIALPLUGIN_H
#define SERIALPLUGIN_H

#ifdef __cplusplus
extern "C" {
#endif

// Platform-specific export macro
#if defined(_WIN32) || defined(_WIN64)
    #define EXPORT __declspec(dllexport)
#else
    #define EXPORT
#endif

/**
 * Opens a serial port connection
 *
 * @param portName The name of the serial port (e.g., "COM3" on Windows, "/dev/cu.usbserial-0001" on macOS)
 * @param baudIndex Index for baud rate:
 *                  0 = 4800
 *                  1 = 9600
 *                  2 = 19200
 *                  3 = 38400
 *                  4 = 57600
 *                  5 = 115200
 *                  default = 300
 * @param debug Enable debug output to console
 * @return 1 if successful, 0 on error
 */
EXPORT int SerialOpen(const char* portName, unsigned char baudIndex, bool debug);

/**
 * Closes the currently open serial port
 */
EXPORT void SerialClose();

/**
 * Writes a single byte to the serial port
 *
 * @param byte The single byte to write
 * @return 1 if successful, 0 on error
 */
EXPORT int SerialWrite(unsigned char byte);

/**
 * Writes two bytes (position and speed) to the serial port
 *
 * @param position The position value to write
 * @param speed The speed value to write
 * @return The number of bytes written (2 on success, 0 on error)
 */
EXPORT int SerialWriteTwo(unsigned char position, unsigned char speed);

/**
 * Reads a single byte from the serial port (non-blocking)
 *
 * @return The byte value (0-255) if data is available, -1 if no data or error
 */
EXPORT int SerialRead();

#ifdef __cplusplus
}
#endif

#endif // SERIALPLUGIN_H
