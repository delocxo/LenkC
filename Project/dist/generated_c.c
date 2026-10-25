#include <stdlib.h>
#include <stdint.h>
typedef struct {
    int32_t *elements;
    unsigned long long count;
    unsigned long long capacity;
} struct_List_1_int_end;
struct_List_1_int_end *_lenkc_function_list_new_int_(unsigned long long _lenkc_capacity_);
void _lenkc_function_list_add_int_(struct_List_1_int_end *_lenkc_list_, int32_t _lenkc_element_);
void _lenkc_function_list_print_all_int_(struct_List_1_int_end *_lenkc_list_);
void _lenkc_function_list_destroy_int_(struct_List_1_int_end *_lenkc_list_);
#include <stdio.h>
void _lenkc_function_println_int_(int32_t _lenkc_value_);
int32_t _lenkc_function_main__(int32_t _lenkc_argc_, char* *_lenkc_argv_);
struct_List_1_int_end *_lenkc_function_malloc_struct_List_1_int_end_(unsigned long long _lenkc_size_);
int32_t *_lenkc_function_malloc_int_(unsigned long long _lenkc_size_);
int32_t *_lenkc_function_realloc_int_(int32_t *_lenkc_old_ptr_, unsigned long long _lenkc_size_);
void _lenkc_function_free_int_(int32_t *_lenkc_data_);
void _lenkc_function_free_struct_List_1_int_end_(struct_List_1_int_end *_lenkc_data_);

struct_List_1_int_end *_lenkc_function_list_new_int_(unsigned long long _lenkc_capacity_) {
    struct_List_1_int_end *_lenkc_list_ = (_lenkc_function_malloc_struct_List_1_int_end_)(1);
    (*_lenkc_list_).elements = (_lenkc_function_malloc_int_)(_lenkc_capacity_);
    (*_lenkc_list_).count = 0;
    (*_lenkc_list_).capacity = _lenkc_capacity_;
    return _lenkc_list_;
}

void _lenkc_function_list_add_int_(struct_List_1_int_end *_lenkc_list_, int32_t _lenkc_element_) {
    if (((*_lenkc_list_).count >= (*_lenkc_list_).capacity)) {
        (*_lenkc_list_).capacity = ((*_lenkc_list_).capacity * 2);
        (*_lenkc_list_).elements = (_lenkc_function_realloc_int_)((*_lenkc_list_).elements, (*_lenkc_list_).capacity);
    }
    (*_lenkc_list_).elements[(*_lenkc_list_).count] = _lenkc_element_;
    (*_lenkc_list_).count = ((*_lenkc_list_).count + 1);
}

void _lenkc_function_list_print_all_int_(struct_List_1_int_end *_lenkc_list_) {
    int32_t _lenkc_i_ = 0;
    while ((_lenkc_i_ < (*_lenkc_list_).count)) {
        (_lenkc_function_println_int_)((*_lenkc_list_).elements[_lenkc_i_]);
        _lenkc_i_ = (_lenkc_i_ + 1);
    }
}

void _lenkc_function_list_destroy_int_(struct_List_1_int_end *_lenkc_list_) {
    (_lenkc_function_free_int_)((*_lenkc_list_).elements);
    (_lenkc_function_free_struct_List_1_int_end_)(_lenkc_list_);
}

void _lenkc_function_println_int_(int32_t _lenkc_value_) {
    printf("%d\n", _lenkc_value_);
}

int32_t _lenkc_function_main__(int32_t _lenkc_argc_, char* *_lenkc_argv_) {
    struct_List_1_int_end *_lenkc_list_ = (_lenkc_function_list_new_int_)(2);
    (_lenkc_function_list_add_int_)(_lenkc_list_, 1);
    (_lenkc_function_list_add_int_)(_lenkc_list_, 2);
    (_lenkc_function_list_add_int_)(_lenkc_list_, 3);
    (_lenkc_function_list_add_int_)(_lenkc_list_, 4);
    (_lenkc_function_list_print_all_int_)(_lenkc_list_);
    (_lenkc_function_list_destroy_int_)(_lenkc_list_);
    return 0;
}

struct_List_1_int_end *_lenkc_function_malloc_struct_List_1_int_end_(unsigned long long _lenkc_size_) {
    struct_List_1_int_end *_lenkc_data_ = ((struct_List_1_int_end *)malloc(sizeof(struct_List_1_int_end) * _lenkc_size_));
    return _lenkc_data_;
}

int32_t *_lenkc_function_malloc_int_(unsigned long long _lenkc_size_) {
    int32_t *_lenkc_data_ = ((int32_t *)malloc(sizeof(int32_t) * _lenkc_size_));
    return _lenkc_data_;
}

int32_t *_lenkc_function_realloc_int_(int32_t *_lenkc_old_ptr_, unsigned long long _lenkc_size_) {
    int32_t *_lenkc_data_ = ((int32_t *)realloc(_lenkc_old_ptr_, sizeof(int32_t) * _lenkc_size_));
    return _lenkc_data_;
}

void _lenkc_function_free_int_(int32_t *_lenkc_data_) {
    free(_lenkc_data_);
}

void _lenkc_function_free_struct_List_1_int_end_(struct_List_1_int_end *_lenkc_data_) {
    free(_lenkc_data_);
}


int main(int argc, char** argv) {
    return _lenkc_function_main__(argc, argv);
}
