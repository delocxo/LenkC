#include <stdlib.h>
#include <stdint.h>
typedef struct struct_List_1_int_end struct_List_1_int_end;
typedef struct struct_List_1_float_end struct_List_1_float_end;

int32_t _lenkc_function_overload_1_main_va_0__();
void _lenkc_function_overload_1_check_va_0__(int _lenkc_passed_, char *_lenkc_name_);

#include <stdio.h>
struct struct_List_1_int_end {
    int32_t value;
};
struct struct_List_1_float_end {
    float value;
};
int32_t _lenkc_failures_ = 0;

int32_t _lenkc_function_overload_1_main_va_0__() {
    _lenkc_function_overload_1_check_va_0__(1, "definition has collection");
    _lenkc_function_overload_1_check_va_0__(1, "List<int> inherits collection");
    _lenkc_function_overload_1_check_va_0__(1, "List<float> inherits collection");
    _lenkc_function_overload_1_check_va_0__(1, "List<int> has its own tag");
    _lenkc_function_overload_1_check_va_0__(!(0), "specialization tags do not leak");
    _lenkc_function_overload_1_check_va_0__(!(0), "specialization tags do not affect definition");
    _lenkc_function_overload_1_check_va_0__(!(0), "missing tag returns false");
    _lenkc_function_overload_1_check_va_0__(1, "fresh definition symbol finds existing entry");
    return _lenkc_failures_;
}

void _lenkc_function_overload_1_check_va_0__(int _lenkc_passed_, char *_lenkc_name_) {
    if (_lenkc_passed_) {
        printf("PASS: %s\n", _lenkc_name_);
    }
    else {
        printf("FAIL: %s\n", _lenkc_name_);
        _lenkc_failures_ += 1;
    }
}


int main(int argc, char** argv) {
    return _lenkc_function_overload_1_main_va_0__();
}
