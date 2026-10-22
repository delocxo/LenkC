#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>
int main() {
    int32_t _variable_i_ = 0;
    int32_t _variable_sum_ = 0;
    int32_t _variable_accepted_ = 0;
    while ((_variable_i_ < 100)) {
        _variable_i_ = (_variable_i_ + 1);
        if (((_variable_i_ % 3) == 0)) {
            continue;
        }
        if ((_variable_i_ > 12)) {
            break;
        }
        _variable_sum_ = (_variable_sum_ + _variable_i_);
        _variable_accepted_ = (_variable_accepted_ + 1);
    }
    printf("Test 1: sum=%d accepted=%d\n", _variable_sum_, _variable_accepted_);
    int32_t _variable_number_ = -(4);
    int32_t _variable_negatives_ = 0;
    int32_t _variable_zeros_ = 0;
    int32_t _variable_small_ = 0;
    int32_t _variable_large_ = 0;
    int32_t _variable_branchScore_ = 0;
    while ((_variable_number_ <= 6)) {
        if ((_variable_number_ < 0)) {
            _variable_negatives_ = (_variable_negatives_ + 1);
            _variable_branchScore_ = (_variable_branchScore_ + 1);
        }
        else if ((_variable_number_ == 0)) {
            _variable_zeros_ = (_variable_zeros_ + 1);
            _variable_branchScore_ = (_variable_branchScore_ + 10);
        }
        else if ((_variable_number_ < 4)) {
            _variable_small_ = (_variable_small_ + 1);
            _variable_branchScore_ = (_variable_branchScore_ + 100);
        }
        else {
            _variable_large_ = (_variable_large_ + 1);
            _variable_branchScore_ = (_variable_branchScore_ + 1000);
        }
        _variable_number_ = (_variable_number_ + 1);
    }
    printf("Test 2: %d %d %d %d\n", _variable_negatives_, _variable_zeros_, _variable_small_, _variable_large_);
    printf("Branch score: %d\n", _variable_branchScore_);
    int32_t _variable_outer_ = 0;
    int32_t _variable_inner_ = 0;
    int32_t _variable_iterations_ = 0;
    while ((_variable_outer_ < 4)) {
        _variable_inner_ = 0;
        while ((_variable_inner_ < 10)) {
            _variable_inner_ = (_variable_inner_ + 1);
            if ((_variable_inner_ == 2)) {
                continue;
            }
            if ((_variable_inner_ == 5)) {
                break;
            }
            _variable_iterations_ = (_variable_iterations_ + 1);
        }
        _variable_outer_ = (_variable_outer_ + 1);
    }
    printf("Test 3: iterations=%d\n", _variable_iterations_);
    int32_t _variable_n_ = 6;
    int32_t _variable_factorial_ = 1;
    while ((_variable_n_ > 1)) {
        _variable_factorial_ = (_variable_factorial_ * _variable_n_);
        _variable_n_ = (_variable_n_ - 1);
    }
    printf("Test 4: factorial=%d\n", _variable_factorial_);
    int32_t _variable_score_ = 85;
    int _variable_enabled_ = 1;
    int32_t _variable_category_ = 0;
    if (_variable_enabled_) {
        if ((_variable_score_ >= 90)) {
            _variable_category_ = 4;
        }
        else if ((_variable_score_ >= 80)) {
            _variable_category_ = 3;
        }
        else if ((_variable_score_ >= 70)) {
            _variable_category_ = 2;
        }
        else {
            _variable_category_ = 1;
        }
    }
    else {
        _variable_category_ = -(1);
    }
    printf("Test 5: category=%d\n", _variable_category_);
    int32_t _variable_untouched_ = 99;
    while (0) {
        _variable_untouched_ = 0;
    }
    printf("Test 6: untouched=%d\n", _variable_untouched_);
    int32_t _variable_counter_ = 0;
    while (1) {
        _variable_counter_ = (_variable_counter_ + 1);
        if ((_variable_counter_ == 25)) {
            break;
        }
    }
    printf("Test 7: counter=%d\n", _variable_counter_);
    int _variable_passed_ = ((((((((((((_variable_sum_ == 48) && (_variable_accepted_ == 8)) && (_variable_negatives_ == 4)) && (_variable_zeros_ == 1)) && (_variable_small_ == 3)) && (_variable_large_ == 3)) && (_variable_branchScore_ == 3314)) && (_variable_iterations_ == 12)) && (_variable_factorial_ == 720)) && (_variable_category_ == 3)) && (_variable_untouched_ == 99)) && (_variable_counter_ == 25));
    if (_variable_passed_) {
        printf("ALL TESTS PASSED!\n");
    }
    else {
        printf("TEST FAILED!\n");
    }

}
