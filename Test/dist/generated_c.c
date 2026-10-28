#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>
int32_t _lenkc_function_main__(int32_t _lenkc_argc_, char* *_lenkc_argv_);

int32_t _lenkc_function_main__(int32_t _lenkc_argc_, char* *_lenkc_argv_) {
    int32_t _lenkc_current_ = 3;
    int32_t _lenkc_combined_ = (1 | 2);
    int32_t _lenkc_all_ = 7;
    printf("Current: %d\n", _lenkc_current_);
    printf("Combined: %d\n", _lenkc_combined_);
    printf("All: %d\n", _lenkc_all_);
    if ((_lenkc_current_ != 3)) {
        return 1;
    }
    if ((_lenkc_combined_ != 3)) {
        return 2;
    }
    if ((_lenkc_all_ != 7)) {
        return 3;
    }
    printf("ENUM TYPE TEST PASSED!\n");
    return 0;
}


int main(int argc, char** argv) {
    return _lenkc_function_main__(argc, argv);
}
