#include <stdlib.h>
#include <stdint.h>
typedef struct struct_Box_1_int_end struct_Box_1_int_end;
typedef struct struct_Box_1_string_end struct_Box_1_string_end;

int32_t _lenkc_function_overload_1_main_va_0__();
int32_t _lenkc_function_overload_1_isHashable_va_0_string_();
int32_t _lenkc_function_overload_1_isHashable_va_0_int_();

#include <stdio.h>
struct struct_Box_1_int_end {
    int32_t value;
};
struct struct_Box_1_string_end {
    char* value;
};

int32_t _lenkc_function_overload_1_main_va_0__() {
    if (1) {
        printf("PASS: string is hashable\n");
    }
    if (1) {
        printf("PASS: repeated tag declarations accumulate\n");
    }
    if (1) {
        printf("PASS: untagged type returns false\n");
    }
    if (1) {
        printf("PASS: nonexistent tag returns false\n");
    }
    if (1) {
        printf("PASS: generic struct has its tag\n");
    }
    if (1) {
        printf("PASS: generic specializations have separate tags\n");
    }
    printf("Generic string check: %d (expected 1)\n", _lenkc_function_overload_1_isHashable_va_0_string_());
    printf("Generic int check: %d (expected 0)\n", _lenkc_function_overload_1_isHashable_va_0_int_());
    return 0;
}

int32_t _lenkc_function_overload_1_isHashable_va_0_string_() {
    int32_t _lenkc_result_ = 0;
    if (1) {
        _lenkc_result_ = 1;
    }
    return _lenkc_result_;
}

int32_t _lenkc_function_overload_1_isHashable_va_0_int_() {
    int32_t _lenkc_result_ = 0;
    return _lenkc_result_;
}


int main(int argc, char** argv) {
    return _lenkc_function_overload_1_main_va_0__();
}
