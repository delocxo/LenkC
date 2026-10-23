#include <stdlib.h>
#include <stdint.h>
float _lenkc_function_add__(float _lenkc_a_, float _lenkc_b_) {
    return (_lenkc_a_ + _lenkc_b_);
}
int32_t _lenkc_function_main__(int32_t _lenkc_argc_, char* *_lenkc_argv_) {
    float _lenkc_x_ = (_lenkc_function_add__)(100, 100);
    return 0;
}

int main(int argc, char** argv) {
    return _lenkc_function_main__(argc, argv);
}
