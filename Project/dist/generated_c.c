#include <stdlib.h>
#include <stdint.h>

void _lenkc_function_variadic_va_0__(int _lenkcreserved_dparam_, ...);
int32_t _lenkc_function_main_va_0__(int32_t _lenkc_argc_, char* *_lenkc_argv_);

void _lenkc_function_variadic_va_0__(int _lenkcreserved_dparam_, ...) {
    int32_t _lenkc_x_ = _lenkcreserved_dparam_;
}

int32_t _lenkc_function_main_va_0__(int32_t _lenkc_argc_, char* *_lenkc_argv_) {
    _lenkc_function_variadic_va_0__(0);
    return 0;
}


int main(int argc, char** argv) {
    return _lenkc_function_main_va_0__(argc, argv);
}
