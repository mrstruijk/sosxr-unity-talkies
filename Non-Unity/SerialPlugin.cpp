// SerialPlugin.cpp 
// Compile as shared library: .dll (Windows) / .bundle (macOS) which needs be placed in a /Plugins folder inside Unity Assets folder. 
// Example compilation commands: 
// Windows (Visual Studio CPP Build Tools), from https://visualstudio.microsoft.com/visual-cpp-build-tools/:
// cl /EHsc /MD /LD SerialPlugin.cpp /Fe:SerialPlugin.dll
// macOS (clang++):
// clang++ -dynamiclib -o SerialPlugin.bundle SerialPlugin.cpp -framework IOKit -framework CoreFoundation

#ifdef _WIN32
    #include <windows.h>
    #define EXPORT __declspec(dllexport)
#else
    #include <fcntl.h>
    #include <unistd.h>
    #include <termios.h>
    #include <string.h>
    #include <errno.h>
    #define EXPORT __attribute__((visibility("default")))
#endif

#include <stdio.h>
#include <iostream>

#define DEBUG_SERIAL 1

extern "C" {

#ifdef _WIN32
    static HANDLE hSerial = INVALID_HANDLE_VALUE;
#else
    static int fd = -1;
#endif

    EXPORT int SerialOpen(const char* portName, int baudRate) {
#ifdef _WIN32
        hSerial = CreateFileA(portName,
            GENERIC_READ | GENERIC_WRITE,
            0, NULL, OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL, NULL);

        if (hSerial == INVALID_HANDLE_VALUE) return 0;

        DCB dcbSerialParams = {0};
        dcbSerialParams.DCBlength = sizeof(dcbSerialParams);

        if (!GetCommState(hSerial, &dcbSerialParams)) {
            CloseHandle(hSerial);
            return 0;
        }

        dcbSerialParams.BaudRate = baudRate;
        dcbSerialParams.ByteSize = 8;
        dcbSerialParams.StopBits = ONESTOPBIT;
        dcbSerialParams.Parity = NOPARITY;

        if (!SetCommState(hSerial, &dcbSerialParams)) {
            CloseHandle(hSerial);
            return 0;
        }

        COMMTIMEOUTS timeouts = {0};
        timeouts.ReadIntervalTimeout = 50;
        timeouts.ReadTotalTimeoutConstant = 50;
        timeouts.ReadTotalTimeoutMultiplier = 10;
        timeouts.WriteTotalTimeoutConstant = 50;
        timeouts.WriteTotalTimeoutMultiplier = 10;

        if (!SetCommTimeouts(hSerial, &timeouts)) {
            CloseHandle(hSerial);
            return 0;
        }

        return 1;
#else
        // Open with blocking mode initially
        fd = open(portName, O_RDWR | O_NOCTTY);
        if (fd == -1) {
            if (DEBUG_SERIAL) std::cerr << "Failed to open " << portName << ": " << strerror(errno) << std::endl;
            return 0;
        }

        struct termios options;
        if (tcgetattr(fd, &options) == -1) {
            if (DEBUG_SERIAL) std::cerr << "tcgetattr failed: " << strerror(errno) << std::endl;
            close(fd);
            fd = -1;
            return 0;
        }

        // Set baud rate
        cfsetispeed(&options, B115200);
        cfsetospeed(&options, B115200);

        // 8N1 mode
        options.c_cflag |= (CLOCAL | CREAD);
        options.c_cflag &= ~PARENB;
        options.c_cflag &= ~CSTOPB;
        options.c_cflag &= ~CSIZE;
        options.c_cflag |= CS8;

        // Raw mode
        options.c_lflag &= ~(ICANON | ECHO | ECHOE | ISIG);
        options.c_iflag &= ~(IXON | IXOFF | IXANY | ICRNL | INLCR);
        options.c_oflag &= ~OPOST;

        // Timing
        options.c_cc[VMIN] = 0;
        options.c_cc[VTIME] = 1;

        if (tcsetattr(fd, TCSANOW, &options) == -1) {
            if (DEBUG_SERIAL) std::cerr << "tcsetattr failed: " << strerror(errno) << std::endl;
            close(fd);
            fd = -1;
            return 0;
        }

        // Critical: Flush buffers and wait
        tcflush(fd, TCIOFLUSH);
        usleep(500000); // 500ms delay - gives Pico time to stabilize

        if (DEBUG_SERIAL) std::cout << "Successfully opened " << portName << std::endl;

        return 1;
#endif
    }

    EXPORT int SerialWrite(const unsigned char* data, int length) {
#ifdef _WIN32
        if (hSerial == INVALID_HANDLE_VALUE) return 0;
        DWORD bytesWritten;
        if (!WriteFile(hSerial, data, length, &bytesWritten, NULL)) return 0;
        return bytesWritten;
#else
        if (fd == -1) {
            if (DEBUG_SERIAL) std::cerr << "SerialWrite: fd is -1" << std::endl;
            return 0;
        }

        int totalWritten = 0;
        int attempts = 0;
        const int maxAttempts = 5;

        while (totalWritten < length && attempts < maxAttempts) {
            int n = write(fd, data + totalWritten, length - totalWritten);

            if (n < 0) {
                if (errno == EINTR) {
                    // Interrupted, just retry
                    continue;
                }
                if (errno == EAGAIN || errno == EWOULDBLOCK) {
                    // Would block, wait and retry
                    usleep(10000); // 10ms
                    attempts++;
                    continue;
                }
                // Real error
                if (DEBUG_SERIAL) std::cerr << "Write error: " << strerror(errno) << std::endl;
                return totalWritten;
            }

            totalWritten += n;

            if (n > 0 && totalWritten < length) {
                // Partial write, small delay before next chunk
                usleep(1000);
            }
        }

        if (DEBUG_SERIAL && totalWritten > 0) {
            std::cout << "Wrote " << totalWritten << " bytes" << std::endl;
        }

        // Small delay after write to let Pico process
        if (totalWritten == length) {
            usleep(5000); // 5ms
        }

        return totalWritten;
#endif
    }

    EXPORT int SerialRead(unsigned char* buffer, int bufferSize) {
#ifdef _WIN32
        if (hSerial == INVALID_HANDLE_VALUE) return 0;
        DWORD bytesRead;
        if (!ReadFile(hSerial, buffer, bufferSize - 1, &bytesRead, NULL)) return 0;
        buffer[bytesRead] = '\0';
        if (DEBUG_SERIAL && bytesRead > 0) std::cout << "Read " << bytesRead << " bytes" << std::endl;
        return bytesRead;
#else
        if (fd == -1) return 0;

        int n = read(fd, buffer, bufferSize - 1);

        if (n < 0) {
            if (errno == EINTR || errno == EAGAIN) {
                return 0; // No data available
            }
            if (DEBUG_SERIAL) std::cerr << "Read error: " << strerror(errno) << std::endl;
            return 0;
        }

        if (n > 0) {
            buffer[n] = '\0';
            if (DEBUG_SERIAL) std::cout << "Read " << n << " bytes" << std::endl;
        }

        return n;
#endif
    }

    EXPORT void SerialClose() {
#ifdef _WIN32
        if (hSerial != INVALID_HANDLE_VALUE) {
            CloseHandle(hSerial);
            hSerial = INVALID_HANDLE_VALUE;
        }
#else
        if (fd != -1) {
            if (DEBUG_SERIAL) std::cout << "Closing serial port" << std::endl;
            close(fd);
            fd = -1;
        }
#endif
    }
}