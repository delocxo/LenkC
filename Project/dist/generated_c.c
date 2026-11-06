#include <stdlib.h>
#include <stdint.h>

int32_t _lenkc_function_overload_1_main_va_0__();
int32_t _lenkc_function_overload_1_crossed_va_0__(int32_t _lenkc_a_, int64_t _lenkc_b_);
int32_t _lenkc_function_overload_2_narrow_va_0__(int16_t _lenkc_value_);
int32_t _lenkc_function_overload_2_fixedTie_va_1__(int32_t _lenkc_value_, ...);
int32_t _lenkc_function_overload_2_variadicTie_va_1__(int64_t _lenkc_first_, int32_t _lenkc_second_, ...);
int32_t _lenkc_function_overload_2_genericTie_va_0_string_(int32_t _lenkc_first_, char* _lenkc_second_);
int32_t _lenkc_function_overload_1_threeWay_va_0__(int32_t _lenkc_a_, int64_t _lenkc_b_, int32_t _lenkc_c_);


int32_t _lenkc_function_overload_1_main_va_0__() {
    _lenkc_function_overload_1_crossed_va_0__(10, ((int64_t)10));
    _lenkc_function_overload_2_narrow_va_0__(((int16_t)10));
    _lenkc_function_overload_2_fixedTie_va_1__(10, 100);
    _lenkc_function_overload_2_variadicTie_va_1__(((int64_t)10), 20, "extra");
    _lenkc_function_overload_2_genericTie_va_0_string_(10, "");
    _lenkc_function_overload_1_threeWay_va_0__(10, ((int64_t)20), 30);
    return 0;
}

int32_t _lenkc_function_overload_1_crossed_va_0__(int32_t _lenkc_a_, int64_t _lenkc_b_) {
    return 1;
}

int32_t _lenkc_function_overload_2_narrow_va_0__(int16_t _lenkc_value_) {
    return 2;
}

int32_t _lenkc_function_overload_2_fixedTie_va_1__(int32_t _lenkc_value_, ...) {
    return 2;
}

int32_t _lenkc_function_overload_2_variadicTie_va_1__(int64_t _lenkc_first_, int32_t _lenkc_second_, ...) {
    return 2;
}

int32_t _lenkc_function_overload_2_genericTie_va_0_string_(int32_t _lenkc_first_, char* _lenkc_second_) {
    return 2;
}

int32_t _lenkc_function_overload_1_threeWay_va_0__(int32_t _lenkc_a_, int64_t _lenkc_b_, int32_t _lenkc_c_) {
    return 1;
}


int main(int argc, char** argv) {
    return _lenkc_function_overload_1_main_va_0__();
}
