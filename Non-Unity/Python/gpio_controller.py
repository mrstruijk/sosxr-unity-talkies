# gpio_controller.py

from machine import Pin, PWM
import sys
import utime

class GPIOController:
    def __init__(self):
        self.pins = {}
        self.pwms = {}
        print("Note: The first connected terminal receives flushed messages. This means that if eg. Thonny is showing GET/SET commands in the console, those commands do not reach their final destination!")

    '''
    This also will put the pin in the dictionary, if it is not already in there.
    '''
    def get_pin_from_dictionary(self, pin_num: int) -> Pin:
        return self.pins.setdefault(pin_num, Pin(pin_num, Pin.OUT))


    def get_pwm_from_dictionary(self, pin_num: int) -> PWM:
        return self.pwms.setdefault(pin_num, PWM(Pin(pin_num)))

    '''
    Set the pin either high or low
    '''
    def set_pin(self, pin_num: int, value: bool):
        value_int = 1 if value else 0 # is safer because some boards like this better.
        self.get_pin_from_dictionary(pin_num).value(value_int)


    def set_pwm_percent(self, pin_num: int, freq: int, duty_percent: int):
        p = self.get_pwm_from_dictionary(pin_num)
        p.freq(freq)
        duty_u16 = int(duty_percent / 100 * 65535)
        p.duty_u16(duty_u16)


    '''
    Get the current pin state (is the pin set HIGH or LOW?)
    '''
    def get_pin_current_value(self, pin_num: int) -> int:
        return self.get_pin_from_dictionary(pin_num).value()


    '''
    Send information out via stdout.
    '''
    def send_line(self, line: str):
        sys.stdout.write(line + "\n")


    '''
    Read the commands we received via stdin, and act accordingly: 
    - Do we need to set a pin high/low?
    - Do we need to provide info on current state of pin?
    '''
    def handle_command(self, line: str):
        parts = line.strip().split(',')
        if not parts or not parts[0]:
            return

        cmd = parts[0].upper()

        try:
            if cmd == "SET" and len(parts) >= 3:
                pin_num, new_value = map(int, parts[1:3])
                self.set_pin(pin_num, new_value)
                self.send_line(f"OK,SET,{pin_num},{new_value}")

            elif cmd == "PWMSET" and len(parts) >= 4:
                pin_num = int(parts[1])
                freq = int(parts[2])
                duty = int(parts[3])
                self.set_pwm_percent(pin_num, freq, duty)
                self.send_line(f"OK,PWMSET,{pin_num},{freq},{duty}")

            elif cmd == "GET" and len(parts) >= 2:
                pin_num = int(parts[1])
                current_value = self.get_pin_current_value(pin_num)
                self.send_line(f"OK,GET,{pin_num},{current_value}")

            elif cmd == "GETALL":
                if not self.pins:
                    self.send_line("OK,NO_PINS_SET")
                else:
                    for pin_num, pin in self.pins.items():
                        current_value = pin.value()
                        self.send_line(f"OK,GET,{pin_num},{current_value}")

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
        for p in self.pwms.values():
            p.deinit()
        print("All PWM pins disabled")
        for pin in self.pins.values():
            pin.off()
        print("All GPIO pins set low.")


if __name__ == "__main__":
    gpio = GPIOController()
    try:
        gpio.run()
    except KeyboardInterrupt:
        gpio.cleanup()