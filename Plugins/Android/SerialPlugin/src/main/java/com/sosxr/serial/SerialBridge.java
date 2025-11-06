package com.sosxr.serial;

import android.app.Activity;
import android.content.Context;
import android.hardware.usb.*;

import com.hoho.android.usbserial.driver.*;
import com.hoho.android.usbserial.util.SerialInputOutputManager;

public class SerialBridge {
    private static UsbSerialPort port;
    private static byte[] readTemp = new byte[4096];

    public static boolean open(Activity activity, int baudRate) {
        UsbManager manager = (UsbManager) activity.getSystemService(Context.USB_SERVICE);

        for (UsbDevice device : manager.getDeviceList().values()) {
            UsbSerialDriver driver = UsbSerialProber.getDefaultProber().probeDevice(device);
            if (driver == null) continue;

            try {
                UsbDeviceConnection conn = manager.openDevice(device);
                if (conn == null) return false;

                port = driver.getPorts().get(0);
                port.open(conn);
                port.setParameters(baudRate, 8, 1, UsbSerialPort.PARITY_NONE);

                return true;
            } catch (Exception e) {
                return false;
            }
        }
        return false;
    }

    public static int write(byte[] data, int len) {
        if (port == null) return 0;

        try {
            return port.write(data, 50);
        } catch (Exception e) {
            return 0;
        }
    }

    public static int read(byte[] buffer, int maxLen) {
        if (port == null) return 0;

        try {
            int n = port.read(readTemp, 50);
            if (n <= 0) return 0;

            int copyLen = Math.min(n, maxLen);
            System.arraycopy(readTemp, 0, buffer, 0, copyLen);
            return copyLen;
        } catch (Exception e) {
            return 0;
        }
    }

    public static void close() {
        if (port != null) {
            try {
                port.close();
            } catch (Exception ignored) {}
        }
        port = null;
    }
}
