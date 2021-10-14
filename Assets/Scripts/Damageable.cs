using UnityEngine;

public interface Damageable
{
    //La fonction lorsqu'on recoit les degats d'une attaque
    void TakeDamage(int damages, Damageable attacker);

    //La fonction a effectue lorsque nos points de vie tombent trop bas
    void OnDeath(Damageable attacker);

    //Lorsqu'on tue quelque chose, on fait quoi ?
    void OnKill();

    //Pour recuperer le rigidbody de l'attaquant, pour les mobs surtout
    Rigidbody GetRigidbody();
}
