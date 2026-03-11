from machine import Pin, PWM
import sys, utime

class GPIOController:
    def __init__(self):
        self.pins = {}  # pin_num -> Pin or PWM object

    def get_pin(self, pin_num: int):
        if pin_num not in self.pins or isinstance(self.pins[pin_num], PWM):
            self.pins[pin_num] = Pin(pin_num, mode=Pin.OUT)
        return self.pins[pin_num]

    def get_pwm(self, pin_num: int):
        if pin_num not in self.pins or not isinstance(self.pins[pin_num], PWM):
            self.pins[pin_num] = PWM(Pin(pin_num))
        return self.pins[pin_num]

    def set_pin(self, pin_num: int, value):
        self.get_pin(pin_num).value(1 if value else 0)

    def get_pin_value(self, pin_num: int):
        pin = self.pins.get(pin_num)
        if pin is None:
            return 0
        if isinstance(pin, PWM):
            # scale 0–65535 → 0–100%
            return pin.duty_u16() * 100 // 65535
        return pin.value()

    def set_pwm(self, pin_num: int, freq: int, duty_percent: int):
        pwm = self.get_pwm(pin_num)
        pwm.freq(freq)
        # scale 0–100% → 0–65535
        duty_u16 = max(0, min(100, duty_percent)) * 65535 // 100
        pwm.duty_u16(duty_u16)


    def stop_pwm(self, pin_num: int):
        pwm = self.pins.get(pin_num)
        if isinstance(pwm, PWM):
            pwm.duty_u16(0)  # just stop output


    def send_line(self, line: str):
        sys.stdout.write(line + "\n")

    def handle_command(self, line: str):
        parts = line.strip().split(',')
        if not parts or not parts[0]:
            return

        cmd = parts[0].upper()

        try:
            if cmd == "SET" and len(parts) >= 3:
                pin_num, val = map(int, parts[1:3])
                self.set_pin(pin_num, val)
                self.send_line(f"OK,SET,{pin_num},{val}")

            elif cmd == "GET" and len(parts) >= 2:
                pin_num = int(parts[1])
                val = self.get_pin_value(pin_num)
                self.send_line(f"OK,GET,{pin_num},{val}")

            elif cmd == "GETALL":
                if not self.pins:
                    self.send_line("OK,NO_PINS_SET")
                else:
                    for n, p in self.pins.items():
                        val = p.duty_u16() if isinstance(p, PWM) else p.value()
                        self.send_line(f"OK,GET,{n},{val}")

            elif cmd == "PWMSET" and len(parts) >= 4:
                pin_num, freq, duty = map(int, parts[1:4])
                self.set_pwm(pin_num, freq, duty)
                self.send_line(f"OK,PWMSET,{pin_num},{freq},{duty}")

            elif cmd == "PWMSTOP" and len(parts) >= 2:
                pin_num = int(parts[1])
                self.stop_pwm(pin_num)
                self.send_line(f"OK,PWMSTOP,{pin_num}")

            elif cmd == "PING":
                self.send_line("OK,PONG,64,I_VALUE_YOU")

            else:
                self.send_line(f"ERR,UNKNOWN_COMMAND,{cmd}")

        except Exception as e:
            self.send_line(f"ERR,EXCEPTION,{type(e).__name__},{e}")

    def run(self):
        buf = ""
        while True:
            c = sys.stdin.read(1)
            if not c:
                utime.sleep_ms(1)
                continue
            if c == '\n':
                if buf:
                    self.handle_command(buf)
                    buf = ""
            else:
                buf += c

    def cleanup(self):
        for pin in self.pins.values():
            if isinstance(pin, PWM):
                pin.deinit()
            else:
                pin.off()


    if __name__ == "__main__": 
        gpio = GPIOController() 
        try: 
            gpio.run() 
        except KeyboardInterrupt: 
            gpio.cleanup()
