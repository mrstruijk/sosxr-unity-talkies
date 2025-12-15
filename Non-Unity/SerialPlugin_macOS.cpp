// SerialPlugin_macOS.cpp
// macOS implementation of Unity Serial Plugin
// Rename file to SerialPlugin_macOS.cpp (stored without because otherwise Unity cannot build) 
//
// Compile with clang++:
// clang++ -dynamiclib -o SerialPlugin.bundle SerialPlugin_macOS.cpp -framework IOKit -framework CoreFoundation

// macOS (inc source file handling)
// cp SerialPlugin_macOS SerialPlugin_macOS.cpp && clang++ -dynamiclib -o SerialPlugin.bundle SerialPlugin_macOS.cpp -framework IOKit -framework CoreFoundation && rm SerialPlugin_macOS.cpp
//
// Place the resulting SerialPlugin.dll in a /Plugins folder in Unity

#include <termios.h>
#include <unistd.h>
#include <fcntl.h>
#include <errno.h>
#include <string.h>
#include <iostream>

#define EXPORT extern "C"

// --------------------
// State
// --------------------

static int fd = -1;
static bool debugSerial = false;

// --------------------
// Helpers
// --------------------

static speed_t baudToSpeed(int baudRate) {
    switch (baudRate) {
        case 4800:   return B4800;
        case 9600:   return B9600;
        case 19200:  return B19200;
        case 38400:  return B38400;
        case 57600:  return B57600;
        case 115200: return B115200;
        default:     return B115200;
    }
}

// --------------------
// SerialOpen
// --------------------

EXPORT int SerialOpen(const char* portName, int baudRate, bool debug) {
    debugSerial = debug;

    fd = open(portName, O_RDWR | O_NOCTTY | O_NONBLOCK);

    if (fd < 0) {
        if (debugSerial) std::cerr << "Failed to open port" << std::endl;
        return 0;
    }

    struct termios options;
    tcgetattr(fd, &options);

    speed_t spd = baudToSpeed(baudRate);

    cfsetispeed(&options, spd);
    cfsetospeed(&options, spd);

    options.c_cflag |= CS8 | CLOCAL | CREAD;
    options.c_iflag = IGNPAR;
    options.c_oflag = 0;
    options.c_lflag = 0;

    // CRITICAL: Set non-blocking read behavior
    options.c_cc[VMIN] = 0;   // Return immediately with whatever is available
    options.c_cc[VTIME] = 0;  // No timeout

    tcflush(fd, TCIFLUSH);
    tcsetattr(fd, TCSANOW, &options);

    // Arduino CDC devices reboot on open → wait
    usleep(800000);

    if (debugSerial) std::cout << "Serial port opened" << std::endl;
    return 1;
}

// --------------------
// SerialClose
// --------------------

EXPORT void SerialClose() {
    if (fd >= 0) close(fd);
    fd = -1;
    if (debugSerial) std::cout << "Serial port closed" << std::endl;
}

// --------------------
// SerialSetBaud
// --------------------

EXPORT int SerialSetBaud(int baudRate) {
    if (fd < 0) return 0;

    struct termios options;
    if (tcgetattr(fd, &options) == -1) return 0;

    speed_t spd = baudToSpeed(baudRate);

    cfsetispeed(&options, spd);
    cfsetospeed(&options, spd);

    if (tcsetattr(fd, TCSANOW, &options) == -1) return 0;

    tcflush(fd, TCIOFLUSH);
    usleep(20000); // allow device to switch baud

    if (debugSerial) std::cout << "Baud rate set to " << baudRate << std::endl;
    return 1;
}

// --------------------
// SerialWriteInternal
// --------------------

int SerialWriteInternal(const unsigned char* data, int length) {
    if (length <= 0) return 0;

    int n = write(fd, data, length);
    return (n < 0) ? 0 : n;
}

// --------------------
// SerialWrite
// --------------------

EXPORT int SerialWrite(unsigned char position, unsigned char speed) {
    unsigned char buf[2] = { position, speed };
    return SerialWriteInternal(buf, 2);
}

// --------------------
// SerialRead - Read one byte
// --------------------
EXPORT int SerialRead() {
    if (fd < 0) {
        return -1;  // Not connected
    }
    
    unsigned char byte = 0;
    int n = read(fd, &byte, 1);
    
    if (n == 1) {
        if (debugSerial) {
            std::cout << "Read byte: " << (int)byte << " (0x" << std::hex << (int)byte << std::dec << ")" << std::endl;
        }
        return (int)byte;  // Return as int (0-255)
    }
    
    // No data available
    return -1;  // Indicates no data
}
