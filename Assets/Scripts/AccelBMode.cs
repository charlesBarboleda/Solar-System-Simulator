public enum AccelBMode // Determines how a_B is calculated inside EIH (NewtonianApprox provides sufficient accuracy)
{
    NewtonianApprox, // aNewton[B] 
    FixedPointIterated // a[B] from previous iteration
}