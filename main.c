#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>
int32_t *_lenkc_function_malloc_int_(unsigned long long _lenkc_size_) {
    int32_t *_lenkc_data_ = ((int32_t *)malloc(sizeof(int32_t) * _lenkc_size_));
    return _lenkc_data_;
}
void _lenkc_function_free_int_(int32_t *_lenkc_data_) {
    free(_lenkc_data_);
}
int32_t _lenkc_function_main__(int32_t _lenkc_argc_, char* *_lenkc_argv_) {
    int32_t *_lenkc_data_ = (_lenkc_function_malloc_int_)(4);
    _lenkc_data_[0] = 10;
    _lenkc_data_[1] = 20;
    _lenkc_data_[2] = 30;
    _lenkc_data_[3] = 40;
    if ((_lenkc_data_[0] != 10)) {
        return 1;
    }
    if ((_lenkc_data_[1] != 20)) {
        return 2;
    }
    if ((_lenkc_data_[2] != 30)) {
        return 3;
    }
    if ((_lenkc_data_[3] != 40)) {
        return 4;
    }
    int32_t *_lenkc_offset_ = (_lenkc_data_ + 1);
    if ((_lenkc_offset_[0] != 20)) {
        return 5;
    }
    if ((_lenkc_offset_[1] != 30)) {
        return 6;
    }
    _lenkc_offset_[1] = 99;
    if ((_lenkc_data_[2] != 99)) {
        return 7;
    }
    int32_t _lenkc_i_ = 1;
    _lenkc_data_[(_lenkc_i_ + 2)] = 123;
    if ((_lenkc_data_[3] != 123)) {
        return 8;
    }
    (_lenkc_function_free_int_)(_lenkc_data_);
    printf("ALL INDEXING TESTS PASSED\n");
    return 0;
}

int main(int argc, char** argv) {
    return _lenkc_function_main__(argc, argv);
}
