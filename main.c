#include <stdlib.h>
#include <stdint.h>
int32_t *_lenkc_function_malloc_int_(unsigned long long _lenkc_size_) {
    int32_t *_lenkc_data_ = ((int32_t *)malloc(sizeof(int32_t) * _lenkc_size_));
    return _lenkc_data_;
}
void _lenkc_function_free_int_(int32_t *_lenkc_data_) {
    free(_lenkc_data_);
}
int32_t _lenkc_function_main__(int32_t _lenkc_argc_, char* *_lenkc_argv_) {
    int32_t *_lenkc_data_ = (_lenkc_function_malloc_int_)(1);
    (_lenkc_function_free_int_)(_lenkc_data_);
    return 0;
}

int main(int argc, char** argv) {
    return _lenkc_function_main__(argc, argv);
}
