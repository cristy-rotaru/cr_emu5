@0
	DW program_start
	DW NMI_handler
	DW ECALL_handler

@100001
str_input:
	STRZ "Input a number: "
str_output:
	STRZ "The number was: "
str_output_hex:
	STRZ "In hex format: 0x"
str_error:
	STRZ "error: "

@200000
program_start:
	li sp, 0x1000
	
infinite_loop:
	addi a0, zero, 5
	la a1, str_input
	ecall
	
	addi a0, zero, 14 # read the number
	ecall
	
	beqz a1, no_error
	mv t0, a1
	
	addi a0, zero, 5
	la a1, str_error
	ecall
	
	addi a0, zero, 8
	mv a1, t0
	ecall
	
	addi a0, zero, 6
	addi a1, zero, '\n'
	ecall
	
	addi a0, zero, 6
	addi a1, zero, '\r'
	ecall
	
	j infinite_loop
	
no_error:
	mv t0, a0

	addi a0, zero, 5
	la a1, str_output
	ecall

	addi a0, zero, 8
	mv a1, t0
	ecall

	addi a0, zero, 6
	addi a1, zero, '\n'
	ecall
	
	addi a0, zero, 6
	addi a1, zero, '\r'
	ecall
	
	addi a0, zero, 5
	la a1, str_output_hex
	ecall
	
	addi a0, zero, 9
	mv a1, t0
	ecall
	
	addi a0, zero, 6
	addi a1, zero, '\n'
	ecall
	
	addi a0, zero, 6
	addi a1, zero, '\r'
	ecall
	
	j infinite_loop

NMI_handler:
	hlt


@F0000000
ECALL_handler:
	beqz a0, _function_0
	addi t0, zero, 1
	beq t0, a0, _function_1
	addi t0, zero, 2
	beq t0, a0, _function_2
	addi t0, zero, 3
	beq t0, a0, _function_3
	addi t0, zero, 4
	beq t0, a0, _function_4
	addi t0, zero, 5
	beq t0, a0, _function_5
	addi t0, zero, 6
	beq t0, a0, _function_6
	addi t0, zero, 7
	beq t0, a0, _function_7
	addi t0, zero, 8
	beq t0, a0, _function_8
	addi t0, zero, 9
	beq t0, a0, _function_9
	addi t0, zero, 10
	beq t0, a0, _function_10
	addi t0, zero, 11
	beq t0, a0, _function_11
	addi t0, zero, 12
	beq t0, a0, _function_12
	addi t0, zero, 13
	beq t0, a0, _function_13
	addi t0, zero, 14
	beq t0, a0, _function_14
	j _invalid_function

_function_0: # does nothing and returns good status
	sw zero, 0xAC(zero) # return status 0 in a1(x11) register
	iret

_function_1: # halts the system
	hlt

_function_2: # memcpy: a1 = src, a2 = dest, a3 = size
	beqz a3, _2_copy_complete
	bltu a1, a2, _2_check_overlap
_2_normal_copy:
	add t0, zero, zero # iterator
_2_normal_copy_loop:
	beq a3, t0, _2_copy_complete # loop exit condition
	add t1, t0, a1 # next source byte address
	add t2, t0, a2 # next destination address
	lb t3, 0(t1) # copy
	sb t3, 0(t2) # paste
	addi t0, t0, 1 # increment iterator
	beqz zero, _2_normal_copy_loop
_2_check_overlap:
	add t1, a1, a3 # calculate the last address of the source + 1
	bgeu a2, t1, _2_normal_copy
_2_reverse_copy: # if the destination pointer is inside source, copy in reverse to prevent data corruption
	mv t0, a3 # start with last byte
_2_reverse_copy_loop:
	beqz t0, _2_copy_complete
	addi t0, t0, -1 # decrement iterator
	add t1, t0, a1 # next source byte address
	add t2, t0, a2 # next destination address
	lb t3, 0(t1) # copy
	sb t3, 0(t2) # paste
	beqz zero, _2_reverse_copy_loop
_2_copy_complete:
	sw zero, 0xAC(zero) # return status 0 in a1(x11) register
	iret

_function_3: # strcpy: a1 = src, a2 = dest | will return 1 if buffers overlap preventing complete memory corruption
	beq a1, a2, _3_copy_complete # if the source and destination are identical skip the copy
	add t1, zero, a1 # source iterator
	add t2, zero, a2 # destination iterator
_3_copy_loop:
	bge a1, a2, _3_skip_overlap_ckeck # memory will not be corrupted if source buffer starts in the middle of the destination buffer
	bge t1, a2, _3_overlap_error # fatal overlap detected | string terminator was destroyed during copy
_3_skip_overlap_ckeck:
	lb t0, 0(t1) # read the byte from source
	sb t0, 0(t2) # write byte to destination
	beqz t0, _3_copy_complete # if the last byte copyed was '\0' jump to complete
	addi t1, t1, 1
	addi t2, t2, 1 # increment pointers
	beqz zero, _3_copy_loop
_3_copy_complete:
	sw zero, 0xAC(zero) # return status 0 in a1(x11) register
	iret
_3_overlap_error:
	addi t3, zero, 1
	sw t3, 0xAC(zero) # return error status
	iret

_function_4: # strlen | returns the length of the string in a0
	mv t1, a1 # first byte of the string
_4_count_loop:
	lb t0, 0(t1) # reads the next byte
	beqz t0, _4_count_complete # if byte is '\0' counting is complete
	addi t1, t1, 1 # increment the interator pointer
	beqz zero, _4_count_loop
_4_count_complete:
	sub t0, t1, a1 # calculate the pointer difference (string length)
	sw t0, 0xA8(zero) # return length in a0(x10)
	sw zero, 0xAC(zero) # return status 0 in a1(x11) register
	iret

_function_5: # print a string
_5_print_loop:
	lb t0, 0(a1) # load the byte
	beqz t0, _5_print_complete # if byte is '\0' printing is complete
	sb t0, 0x11D(zero) # print the character
	addi a1, a1, 1 # advance the pointer
	beqz zero, _5_print_loop
_5_print_complete:
	sw zero, 0xAC(zero) # return status 0 in a1(x11) register
	iret

_function_6: # print a char
	sb a1, 0x11D(zero) # print the char in a1
	sw zero, 0xAC(zero) # return status 0 in a1(x11) register
	iret

_function_7: # print a signed integer
	lui t0, 0x80000
	and t0, t0, a1 # extract sign bit
	beqz t0, _7_number_is_positive
	addi t0, zero, '-'
	sb t0, 0x11D(zero) # print - (minus sign)
	neg a1, a1 # calculate absolute value
_7_number_is_positive:
_function_8: # print an unsigned integer | same code is used as in function 7, but sign check is skipped
	or t1, zero, zero # digit counter
	addi t2, zero, 10 # divider
_7_8_divide_loop:
	remu t0, a1, t2 # extract last digit
	divu a1, a1, t2 # reduce last digit
	addi t0, t0, '0' # convert to char
	sb t0, 0x200(t1) # save digit
	addi t1, t1, 1 # increment digit counter
	bnez a1, _7_8_divide_loop # if not 0, repeat digit extraction
_7_8_9_print_loop: # print digits in reverse order
	addi t1, t1, -1 # go back one position
	lb t0, 0x200(t1) # read the stored digit
	sb t0, 0x11D(zero) # print the digit
	bnez t1, _7_8_9_print_loop # if not all digits have been printed
	sw zero, 0xAC(zero) # return status 0 in a1(x11) register
	iret

_function_9: # print integer in HEX format
	and t1, zero, zero # digit counter
	addi t2, zero, 9 # compare to this
_9_extract_loop:
	andi t0, a1, 0xF # extract last hex digit
	srli a1, a1, 4 # reduce last hex digit
	bgt t0, t2, _9_greater_than_9 # if 10 or higher use different conversion
	addi t0, t0, '0' # convert to char
	beqz zero, _9_save_converted_char
_9_greater_than_9:
	addi t0, t0, 55 # 'A' - 10 | convert to char for digits greater than 9
_9_save_converted_char:
	sb t0, 0x200(t1) # save digit
	addi t1, t1, 1 # increment digit counter
	bnez a1, _9_extract_loop # if not 0, repeat digit extraction
	beqz zero, _7_8_9_print_loop # use the same print code as in functions 7 and 8 to reduce code size

_function_10: # read a string from console and place it at the pointer in a1 | will return status in a1
	mv fp, a1 # parameter for read function (place string here)
	jal ra, _read_string_from_console # call routine that would read the string
	sw s1, 0xAC(zero) # return status in a1
	iret

_function_11: # read a char from the console and return it in a0
	addi fp, zero, 0x2FF # where to store the read string
	jal ra, _read_string_from_console # read a string from terminal
	beqz s1, _11_read_ok # if read returned with no error continue with extracting one char
	sw s1, 0xAC(zero) # return error code in a1
	iret
_11_read_ok:
	lbu t0, 0x2FF(zero) # read the first byte from the string
	beqz t0, _11_empty_string_inserted # if the string is empty
	lbu t1, 0x300(zero) # read second byte of the string
	bnez t1, _11_string_too_long # if second byte is not '\0'
	sw t0, 0xA8(zero) # return read char in a0
	sw zero, 0xAC(zero) # return status 0 in a1
	iret
_11_empty_string_inserted:
	addi t0, zero, 4 # error code 4 (empty string gave as input)
	sw t0, 0xAC(zero) # return error code in a1
	iret
_11_string_too_long:
	addi t0, zero, 1 # error code 1 (string too long)
	sw t0, 0xAC(zero) # return error code in a1
	iret

_function_12: # read a signed integer
	addi fp, zero, 0x2FF # where to store the read string
	jal ra, _read_string_from_console # read a string from terminal
	beqz s1, _12_read_ok # if read returned with no error continue with extracting one char
	sw s1, 0xAC(zero) # return error code in a1
	iret
_12_read_ok:
	lbu t0, 0x2FF(zero) # read the first byte (sign)
	beqz t0, _12_13_14_string_is_empty # if no characters were given
	add t6, zero, zero # sign (0 = positive | 1 = negative)
	addi fp, zero, 0x2FF
	addi t1, zero, '-' # minus sign
	bne t0, t1, _12_number_is_positive # if there's no minus sign at the beginning
	addi t6, t6, 1 # set the sign
	addi fp, fp, 1 # conversion will start from the second byte
_12_number_is_positive:
	jal ra, _convert_to_unsigned # call function which will make the conversion and return the number in s2
	beqz s1, _12_conversion_ok # if conversion was successful
	sw s1, 0xAC(zero) # return error code in a1
	iret
_12_conversion_ok:
	lui t0, 0x80000 # sign bit
	and t0, t0, s2 # extract the sign bit
	bnez t6, _12_number_is_negative
	bnez t0, _12_number_too_big # number can't be encoded on 32 bits
	sw s2, 0xA8(zero) # return the converted number in a0
	sw zero, 0xAC(zero) # return status 0
	iret
_12_number_is_negative:
	beqz t0, _12_number_is_in_range
	not t0, t0 # all bits except sign bit
	and t0, t0, s2
	bnez t0, _12_number_too_big # number can't be encoded on 32 bits
_12_number_is_in_range:
	neg s2, s2 # negate the number
	sw s2, 0xA8(zero) # return the converted number in a0
	sw zero, 0xAC(zero) # return status 0
	iret
_12_number_too_big:
	addi t0, zero, 5 # error code 5 (number outside of int32 range)
	sw t0, 0xAC(zero) # return error code in a1
	iret
_12_13_14_string_is_empty:
	addi t0, zero, 4 # error code 4 (empty string gave as input)
	sw t0, 0xAC(zero) # return error code in a1
	iret

_function_13: # read an unsigned integer from the terminal
	addi fp, zero, 0x2FF # where to store the read string
	jal ra, _read_string_from_console # read a string from terminal
	beqz s1, _13_read_ok # if read returned with no error continue with extracting one char
	sw s1, 0xAC(zero) # return error code in a1
	iret
_13_read_ok:
	lbu t0, 0x2FF(zero) # read the first byte (sign)
	beqz t0, _12_13_14_string_is_empty # if no characters were given
	addi fp, zero, 0x2FF # where the string is located
	jal ra, _convert_to_unsigned # call function which will make the conversion
	bnez s1, _13_conversion_failed # if status is not 0, don't return the conversion result
	sw s2, 0xA8(zero) # return the converted number in a0
_13_conversion_failed:
	sw s1, 0xAC(zero) # return error code in a1
	iret

_function_14: # read an integer in hex format
	addi fp, zero, 0x2FF # where to store the read string
	jal ra, _read_string_from_console # read a string from terminal
	beqz s1, _14_read_ok # if read returned with no error continue with extracting one char
	sw s1, 0xAC(zero) # return error code in a1
	iret
_14_read_ok:
	lbu t0, 0(fp) # read the first character
	beqz t0, _12_13_14_string_is_empty # string is empty | reuse the same code as 12 and 13
	add s2, zero, zero # the number will be calculated in s2
	lui t2, 0xF0000 # upper 4 bits
_14_conversion_loop:
	lbu t0, 0(fp) # read a character
	beqz t0, _14_conversion_complete # if we hit string terminator
	addi t1, zero, '9'
	bgt t0, t1, _14_not_a_digit
	addi t1, zero, '0'
	blt t0, t1, _14_not_a_digit
	sub t0, t0, t1 # convert to int
	beqz zero, _14_converted_a_digit # jump to the concatenation code
_14_not_a_digit:
	addi t1, zero, 'F'
	bgt t0, t1, _14_not_capital_hex
	addi t1, zero, 'A'
	blt t0, t1, _14_not_capital_hex
	sub t0, t0, t1 # convert to int
	addi t0, t0, 10 # add offset
	beqz zero, _14_converted_a_digit
_14_not_capital_hex:
	addi t1, zero, 'f'
	bgt t0, t1, _14_invalid_hex_character
	addi t1, zero, 'a'
	blt t0, t1, _14_invalid_hex_character
	sub t0, t0, t1 # convert to int
	addi t0, t0, 10 # add offset
_14_converted_a_digit:
	and t1, s2, t2 # extract upper 4 bits
	bnez t1, _14_number_too_big # the number doesn't fit in 32 bits
	slli s2, s2, 4 # shift by 4 to make room for next digit
	or s2, s2, t0 # put the last converted digit at the end
	addi fp, fp, 1 # increment the pointer
	beqz zero, _14_conversion_loop # go back to convert the next digit
_14_invalid_hex_character:
	addi t0, zero, 2 # error code 2 (invalid character)
	sw t0, 0xAC(zero) # return error code in a1
	iret
_14_number_too_big:
	addi t0, zero, 5 # error code 5 (number too big)
	sw t0, 0xAC(zero) # return error code in a1
	iret
_14_conversion_complete:
	sw zero, 0xAC(zero) # status 0 (conversion succesful) in a1
	sw s2, 0xA8(zero) # return result in a0
	iret

_invalid_function:
	ori t1, zero, -1
	sw t1, 0xAC(zero) # return invalid function status in a1(x11) register
	iret

_read_string_from_console:
	lbu t6, 0x11C(zero) # read console status
	andi t6, t6, 0x80 # save interrupt bit to restore later
	addi t0, zero, 0x40 # clear buffer bit
	sb t0, 0x11C(zero) # clear input buffer and disable interrupt from terminal
	add t1, zero, zero # character counter
	addi t2, zero, 255 # 255 + '\0' string length limit
__read_loop:
	bgt t1, t2, __stream_too_long # if the limit was surpassed
__wait_for_char:
	lb t0, 0x11C(zero) # terminal status | bits [6:0] containt the number of characters in the queue and bit 7 we know is 0
	beqz t0, __wait_for_char # if there are no characters in the buffer, keep waiting
	lbu t0, 0x11D(zero) # read character from terminal queue
	addi t3, zero, ' ' # first printable char
	bge t0, t3, __valid_string_char # character can pe put in string
	addi t3, zero, '\t' # character is a tab
	beq t0, t3, __is_tab # tabs will be ignored
	addi t3, zero, '\b' # backslash
	beq t0, t3, __delete_a_char # delete last character read if the buffer is not empty
	addi t3, zero, '\n' # new line
	beq t0, t3, __commit_string # terminate the string and return
	addi t3, zero, '\r' # carrige return
	beq t0, t3, __semi_terminated_line # will have to wait for \n
	addi s1, zero, 2 # return code 2 (invalid control character received)
	sb t6, 0x11C(zero) # restore interrupt bit
	ret # return
__valid_string_char:
	add t4, fp, t1 # calculate byte address
	sb t0, 0(t4) # store the byte
	sb t0, 0x11D(zero) # echo it back to the terminal
	addi t1, t1, 1 # increment the counter
__is_tab:
	beqz zero, __read_loop # go back to read another character
__delete_a_char:
	beqz t1, __buffer_is_empty # there's nothing to delete
	addi t1, t1, -1 # move the pointer back
	sb t0, 0x11D(zero) # write backslash to terminal
__buffer_is_empty:
	beqz zero, __read_loop # go back to read another character
__commit_string:
	add t4, fp, t1 # calculate byte address
	sb zero, 0(t4) # store string terminator
	sb t0, 0x11D(zero) # write \n
	addi t0, zero, '\r'
	sb t0, 0x11D(zero) # write \r
	add s1, zero, zero # return code 0 (success)
	sb t6, 0x11C(zero) # restore interrupt bit
	ret # go back to the ecall that requested a string
__semi_terminated_line:
	addi t3, zero, '\n'
__wait_for_new_line:
	lb t0, 0x11C(zero) # terminal status
	beqz t0, __wait_for_new_line # if there are no characters in the buffer, keep waiting
	lbu t0, 0x11D(zero) # read character from terminal queue
	beq t0, t3, __commit_string # correct line termination
	addi s1, zero, 3 # return code 3 (invalid line termination)
	sb t6, 0x11C(zero) # restore interrupt bit
	ret
__stream_too_long:
	addi s1, zero, 1 # return code 1 (string length limit exceeded)
	sb t6, 0x11C(zero) # restore interrupt bit
	ret

_convert_to_unsigned: # will try to convert a string in an unsigned 32bit number | the caller must ensure that the string is not empty
	add s2, zero, zero # target register | the converted number will be calculated here
	li t1, 429496729 # largest uint32 divided by 10
	addi t2, zero, -6 # (4294967290) largest uint32 without the last digit
	addi t3, zero, 10 # will be used to multiply
	addi t4, zero, 5 # the last digit of the largest uint32
__conversion_loop:
	lbu t0, 0(fp) # read the next character
	beqz t0, __conversion_finished # if we hit a string terminator ('\0')
	addi t5, zero, '9'
	bgt t0, t5, __invalid_numeric_character # if the character is not a digit
	addi t5, zero, '0'
	blt t0, t5, __invalid_numeric_character
	bgtu s2, t1, __number_too_big # will overflow over maxint
	sub t0, t0, t5 # convert digit to int
	mul s2, s2, t3 # multiply the current number by 10
	bltu s2, t2, __small_enough # did not reach critical value
	bgt t0, t4, __number_too_big # if the number cannot be encoded on 32bits
__small_enough:
	add s2, s2, t0 # add the last digit
	addi fp, fp, 1 # increment the pointer
	beqz zero, __conversion_loop # convert next character
__conversion_finished:
	add s1, zero, zero # no error
	ret
__invalid_numeric_character:
	addi s1, zero, 2 # invalid character
	ret
__number_too_big:
	addi s1, zero, 5 # number too big
	ret