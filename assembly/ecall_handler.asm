@0
	DW program_start
	DW NMI_handler
	DW ECALL_handler

@C0001000
	DB 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 

@200000
program_start:
	li sp, 0x1000
	
	addi a0, zero, 2
	li a1, 0xC0001000
	li a2, 0xC0000FF0
	ecall
	li a1, 0xC0001000
	li a2, 0xC0000FF0
	addi a3, zero, 10
	ecall
	li a1, 0xC0001000
	li a2, 0xC0001020
	addi a3, zero, 10
	ecall
	li a1, 0xC0001000
	li a2, 0xC0001004
	addi a3, zero, 10
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

_invalid_function:
	ori t1, zero, -1
	sw t1, 0xAC(zero) # return invalid function status in a1(x11) register
	iret