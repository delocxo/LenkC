#include <stdlib.h>
#include <stdint.h>

#include <stdio.h>


typedef struct
{
    char* first_name;
    char* last_name;
} Name;

int32_t _lenkc_function_main__() {
    Name _lenkc_name_ = (Name){"John", "Doe"};
    printf("Your name is %s %s.", (_lenkc_name_).first_name, (_lenkc_name_).last_name);
    return 0;
}

int main(int argc, char** argv) {
    return _lenkc_function_main__();
}
