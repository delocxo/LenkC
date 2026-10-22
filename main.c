#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>
int main() {
    int32_t _variable_original_ = 42;
    double _variable_decimal_ = 7.75;
    int32_t _variable_truncated_ = ((int32_t)_variable_decimal_);
    float _variable_converted_ = ((float)_variable_original_);
    double _variable_divided_ = (((double)_variable_original_) / 4);
    int8_t _variable_small_ = ((int8_t)120);
    int32_t _variable_widened_ = (((int32_t)_variable_small_) + 5);
    char _variable_letter_ = ((char)65);
    int32_t _variable_ascii_ = ((int32_t)_variable_letter_);
    int _variable_zero_ = ((int)0);
    int _variable_nonzero_ = ((int)-(9));
    int32_t _variable_nested_ = ((int32_t)(((double)15) / 2));
    int32_t _variable_multiplied_ = (((int32_t)5.9) * ((int32_t)2.1));
    int32_t _variable_value_ = 111;
    int32_t* _variable_pointer_ = &_variable_value_;
    void* _variable_opaque_ = ((void*)_variable_pointer_);
    int32_t* _variable_restored_ = ((int32_t*)_variable_opaque_);
    *_variable_restored_ = 222;
    int _variable_pointerCheck_ = ((_variable_restored_ == _variable_pointer_) && (_variable_value_ == 222));
    printf("Truncated: %d\n", _variable_truncated_);
    printf("Converted: %f\n", _variable_converted_);
    printf("Divided: %f\n", _variable_divided_);
    printf("Widened: %d\n", _variable_widened_);
    printf("Character: %c (%d)\n", _variable_letter_, _variable_ascii_);
    printf("Booleans: %d %d\n", _variable_zero_, _variable_nonzero_);
    printf("Nested: %d\n", _variable_nested_);
    printf("Multiplied: %d\n", _variable_multiplied_);
    printf("Pointer value: %d\n", _variable_value_);
    int _variable_passed_ = ((((((((((_variable_truncated_ == 7) && (_variable_converted_ == 42)) && (_variable_divided_ == 10.5)) && (_variable_widened_ == 125)) && (_variable_ascii_ == 65)) && !(_variable_zero_)) && _variable_nonzero_) && (_variable_nested_ == 7)) && (_variable_multiplied_ == 10)) && _variable_pointerCheck_);
    if (_variable_passed_) {
        printf("ALL CAST TESTS PASSED!\n");
    }
    else {
        printf("CAST TEST FAILED!\n");
    }

}
