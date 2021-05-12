@0
	DW program_start
	DW NMI_handler
	DW ECALL_handler

@C0001000
string1:
	STRZ "copy this string\n"
string2:
	STRZ "what is the length of this string\n"
byte:
	DH 0

@200000
program_start:
	li sp, 0x1000
	
	addi a0, zero, 5
	la a1, string1
	ecall
	
	addi a0, zero, 6
	addi a1, zero, '\r'
	ecall
	
	addi a0, zero, 5
	la a1, string2
	ecall
	
	addi a0, zero, 6
	addi a1, zero, '\r'
	ecall
	
	addi a0, zero, 5
	la a1, byte
	addi a1, a1, 1
	ecall
	
	addi a0, zero, 6
	addi a1, zero, 65
	ecall
	
	addi a0, zero, 1
	ecall

NMI_handler:
	iret


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

_invalid_function:
	ori t1, zero, -1
	sw t1, 0xAC(zero) # return invalid function status in a1(x11) register
	iret