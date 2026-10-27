#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>
typedef union {
    int32_t value;
    char* error;
} union_ResultData_2_int_string_end;
typedef struct {
    int isOk;
    union_ResultData_2_int_string_end data;
} struct_Result_2_int_string_end;
struct_Result_2_int_string_end _lenkc_function_Ok_int_string_(int32_t _lenkc_value_);
struct_Result_2_int_string_end _lenkc_function_Err_int_string_(char* _lenkc_error_);
struct_Result_2_int_string_end _lenkc_function_divide__(int32_t _lenkc_a_, int32_t _lenkc_b_);
void _lenkc_function_println_int_(int32_t _lenkc_value_);
void _lenkc_function_println_string_(char* _lenkc_value_);
int32_t _lenkc_function_main__();

struct_Result_2_int_string_end _lenkc_function_Ok_int_string_(int32_t _lenkc_value_) {
    return (struct_Result_2_int_string_end){1, (union_ResultData_2_int_string_end){ .value = _lenkc_value_ }};
}

struct_Result_2_int_string_end _lenkc_function_Err_int_string_(char* _lenkc_error_) {
    return (struct_Result_2_int_string_end){0, (union_ResultData_2_int_string_end){ .error = _lenkc_error_ }};
}

struct_Result_2_int_string_end _lenkc_function_divide__(int32_t _lenkc_a_, int32_t _lenkc_b_) {
    if ((_lenkc_b_ == 0)) {
        return (_lenkc_function_Err_int_string_)("Cannont divide by zero");
    }
    return (_lenkc_function_Ok_int_string_)((_lenkc_a_ / _lenkc_b_));
}

void _lenkc_function_println_int_(int32_t _lenkc_value_) {
    printf("%d\n", _lenkc_value_);
}

void _lenkc_function_println_string_(char* _lenkc_value_) {
    printf("%s\n", _lenkc_value_);
}

int32_t _lenkc_function_main__() {
    struct_Result_2_int_string_end _lenkc_result_ = (_lenkc_function_divide__)(20, 0);
    if ((_lenkc_result_).isOk) {
        int32_t _lenkc_answer_ = ((_lenkc_result_).data).value;
        (_lenkc_function_println_int_)(_lenkc_answer_);
        return _lenkc_answer_;
    }
    else {
        char* _lenkc_error_ = ((_lenkc_result_).data).error;
        (_lenkc_function_println_string_)(_lenkc_error_);
        return 1;
    }
    return 0;
}


int main(int argc, char** argv) {
    return _lenkc_function_main__();
}
