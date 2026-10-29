#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>
int32_t _lenkc_Math_function_add__(int32_t _lenkc_a_, int32_t _lenkc_b_);
int32_t _lenkc_Math_function_distance__(int32_t _lenkc_a_, int32_t _lenkc_b_);
int32_t _lenkc_First_function_value__();
int32_t _lenkc_Second_function_value__();
int32_t _lenkc_function_value__();
int32_t _lenkc_Values_function_identity_int_(int32_t _lenkc_input_);
int _lenkc_Values_function_identity_bool_(int _lenkc_input_);
int32_t _lenkc_Values_function_first_int_(int32_t _lenkc_a_, int32_t _lenkc_b_);
int32_t _lenkc_Checks_function_equal__(int32_t _lenkc_actual_, int32_t _lenkc_expected_);
int32_t _lenkc_function_main__();

int32_t _lenkc_Math_function_add__(int32_t _lenkc_a_, int32_t _lenkc_b_) {
    return (_lenkc_a_ + _lenkc_b_);
}

int32_t _lenkc_Math_function_distance__(int32_t _lenkc_a_, int32_t _lenkc_b_) {
    return (abs)((_lenkc_a_ - _lenkc_b_));
}

int32_t _lenkc_First_function_value__() {
    return 10;
}

int32_t _lenkc_Second_function_value__() {
    return 20;
}

int32_t _lenkc_function_value__() {
    return 30;
}

int32_t _lenkc_Values_function_identity_int_(int32_t _lenkc_input_) {
    return _lenkc_input_;
}

int _lenkc_Values_function_identity_bool_(int _lenkc_input_) {
    return _lenkc_input_;
}

int32_t _lenkc_Values_function_first_int_(int32_t _lenkc_a_, int32_t _lenkc_b_) {
    return _lenkc_a_;
}

int32_t _lenkc_Checks_function_equal__(int32_t _lenkc_actual_, int32_t _lenkc_expected_) {
    (printf)("actual=%d expected=%d\n", _lenkc_actual_, _lenkc_expected_);
    if ((_lenkc_actual_ != _lenkc_expected_)) {
        return 1;
    }
    return 0;
}

int32_t _lenkc_function_main__() {
    int32_t _lenkc_failures_ = 0;
    (printf)("Namespaced functions:\n");
    _lenkc_failures_ = (_lenkc_failures_ + (_lenkc_Checks_function_equal__)((_lenkc_Math_function_add__)(12, 8), 20));
    _lenkc_failures_ = (_lenkc_failures_ + (_lenkc_Checks_function_equal__)((_lenkc_Math_function_distance__)(10, 35), 25));
    (printf)("Same name, different namespaces:\n");
    _lenkc_failures_ = (_lenkc_failures_ + (_lenkc_Checks_function_equal__)((_lenkc_First_function_value__)(), 10));
    _lenkc_failures_ = (_lenkc_failures_ + (_lenkc_Checks_function_equal__)((_lenkc_Second_function_value__)(), 20));
    _lenkc_failures_ = (_lenkc_failures_ + (_lenkc_Checks_function_equal__)((_lenkc_function_value__)(), 30));
    (printf)("Generic functions:\n");
    _lenkc_failures_ = (_lenkc_failures_ + (_lenkc_Checks_function_equal__)((_lenkc_Values_function_identity_int_)(42), 42));
    _lenkc_failures_ = (_lenkc_failures_ + (_lenkc_Checks_function_equal__)((_lenkc_Values_function_first_int_)(17, 99), 17));
    int _lenkc_flag_ = (_lenkc_Values_function_identity_bool_)(1);
    if (!(_lenkc_flag_)) {
        _lenkc_failures_ = (_lenkc_failures_ + 1);
    }
    (printf)("Nested calls with externs:\n");
    _lenkc_failures_ = (_lenkc_failures_ + (_lenkc_Checks_function_equal__)((_lenkc_Math_function_add__)((abs)(-(7)), (_lenkc_Values_function_identity_int_)((_lenkc_Second_function_value__)())), 27));
    (printf)("Failures: %d\n", _lenkc_failures_);
    return _lenkc_failures_;
}


int main(int argc, char** argv) {
    return _lenkc_function_main__();
}
