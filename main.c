#include <stdlib.h>
#include <stdint.h>

#include <stdio.h>

typedef struct {
    int32_t value;
} struct_Box_1_int_end;
typedef struct {
    float value;
} struct_Box_1_float_end;
int _lenkc_function_same_type_int_int_();
int _lenkc_function_same_type_int_float_();
int32_t _lenkc_function_main__();

int _lenkc_function_same_type_int_int_() {
    return 1;
}

int _lenkc_function_same_type_int_float_() {
    return 0;
}

int32_t _lenkc_function_main__() {
    if (!(1)) {
        return 1;
    }
    if (0) {
        return 2;
    }
    if (!(1)) {
        return 3;
    }
    if (0) {
        return 4;
    }
    if (0) {
        return 5;
    }
    if (!(1)) {
        return 6;
    }
    if (0) {
        return 7;
    }
    if (!((_lenkc_function_same_type_int_int_)())) {
        return 8;
    }
    if ((_lenkc_function_same_type_int_float_)()) {
        return 9;
    }
    puts("All typeis checks passed.");
    return 0;
}


int main(int argc, char** argv) {
    return _lenkc_function_main__();
}
