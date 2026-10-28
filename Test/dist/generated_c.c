#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>
int32_t _lenkc_function_main__();

int32_t _lenkc_function_main__() {
    (printf)("Hello from named externs! %d\n", 42);
    return 0;
}


int main(int argc, char** argv) {
    return _lenkc_function_main__();
}
