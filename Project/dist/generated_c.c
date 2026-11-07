#include <stdlib.h>
#include <stdint.h>
typedef struct struct_Box_1_int_end struct_Box_1_int_end;

int32_t _lenkc_function_overload_1_main_va_0__();
int32_t _lenkc_function_overload_1_copy_va_0_int_(int32_t _lenkc_input_);
float _lenkc_function_overload_1_convert_va_0_float_(int32_t _lenkc_input_);
struct_Box_1_int_end _lenkc_function_overload_1_wrap_va_0_int_(int32_t _lenkc_input_);

#include <stdio.h>
struct struct_Box_1_int_end {
    int32_t value;
};

int32_t _lenkc_function_overload_1_main_va_0__() {
    printf("Generic local: %d (expected 42)\n", _lenkc_function_overload_1_copy_va_0_int_(42));
    printf("Generic cast: %.1f (expected 7.0)\n", _lenkc_function_overload_1_convert_va_0_float_(7));
    struct_Box_1_int_end _lenkc_box_ = _lenkc_function_overload_1_wrap_va_0_int_(99);
    printf("Nested generic type: %d (expected 99)\n", (_lenkc_box_).value);
    int32_t _lenkc_T_ = 40;
    _lenkc_T_ += 2;
    int32_t _lenkc_U_ = 5;
    _lenkc_U_ += 1;
    printf("T scope restored: %d (expected 42)\n", _lenkc_T_);
    printf("U scope restored: %d (expected 6)\n", _lenkc_U_);
    return 0;
}

int32_t _lenkc_function_overload_1_copy_va_0_int_(int32_t _lenkc_input_) {
    int32_t _lenkc_local_ = _lenkc_input_;
    return _lenkc_local_;
}

float _lenkc_function_overload_1_convert_va_0_float_(int32_t _lenkc_input_) {
    float _lenkc_local_ = ((float)_lenkc_input_);
    return _lenkc_local_;
}

struct_Box_1_int_end _lenkc_function_overload_1_wrap_va_0_int_(int32_t _lenkc_input_) {
    struct_Box_1_int_end _lenkc_box_ = (struct_Box_1_int_end){_lenkc_input_};
    return _lenkc_box_;
}


int main(int argc, char** argv) {
    return _lenkc_function_overload_1_main_va_0__();
}
