#include <stdlib.h>
#include <stdint.h>

int32_t _lenkc_function_overload_1_main_va_0__(int32_t _lenkc_argc_, char **_lenkc_argv_);
int32_t _lenkc_function_overload_0_lambda_0_va_0__(int32_t _lenkc_a_, int32_t _lenkc_b_);


int32_t _lenkc_function_overload_1_main_va_0__(int32_t _lenkc_argc_, char **_lenkc_argv_) {
    int32_t (*_lenkc_add_)(int32_t, int32_t) = _lenkc_function_overload_0_lambda_0_va_0__;
    _lenkc_add_(100, 100);
    return 0;
}

int32_t _lenkc_function_overload_0_lambda_0_va_0__(int32_t _lenkc_a_, int32_t _lenkc_b_) {
    return (_lenkc_a_ + _lenkc_b_);
}


int main(int argc, char** argv) {
    return _lenkc_function_overload_1_main_va_0__(argc, argv);
}
