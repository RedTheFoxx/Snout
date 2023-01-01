
# Snout - Bot utilitaire

Snout est un ensemble de fonctionnalités utilitaires et de divertissement destinées à un usage privé déployées sur un bot Discord. 
Il fonctionne sur la base de .NET 7.0 et implémente la librairie Discord.NET.

Sa toute première fonctionnalité est implémentée dans le  **core**, elle consiste 
en un ensemble de requêtes HTTP dirigées vers Battlemetrics.com afin de récupérer le 
statut des serveurs Français du jeu *Hell  Let Loose™*. Elle n'est pas directement détachable.

Le reste est divisé en plugins stockés dans le dossier "Modules" ajoutés au fil des 
mise à jour.
Ses fonctions reposent sur l'utilisation du nouveau système de *slash-commands* implémenté par Discord et
qui facilite ses interactions sans code superflu.

Il est essentiellement développé de façon asynchrone dans son exécution.


## Commandes

**Commandes généralistes**
- **/ping** : renvoie le ping de la gateway API Discord.
- **/register** : inscrit un utilisateur dans la base de données de Snout, utilisée dans les modules.
- **/unregister** : retire un utilisateur de la base de données de Snout

**Commandes du banking**
- **/account** : créer un nouveau compte bancaire et l'assigne à un utilisateur (/register non requis)
- **/editaccount** : éditer les paramètres d'un compte bancaire, tels que la limite de découvert, les frais de service ou le taux d'intérêt.
- **/myaccounts** : afficher le statut de ses comptes bancaires. (résultats en messages privés)
- **/checkaccounts** : vérifier le statut des comptes bancaires d'un utilisateur. (résultats en messages privés)
- **/deposit** : ajouter de l'argent à un compte bancaire.
- **/withdraw** : retirer de l'argent d'un compte bancaire.
- **/transfer** : faire un virement entre deux comptes.

**Commandes du fetcher** *(Hell Let Loose™ uniquement)*
- **/add** : permet d'ajouter un nouveau serveur à l'auto-fetcher par utilisation de son URL battlemetrics.
- **/stop** : interrompt l'auto-fetcher de manière globale et purge la liste des canaux de diffusion.
- **/fetch** : assigne l'auto-fetcher au canal ciblé par la commande (+ déclenche ce premier) et si il était déjà actif, se contente d'ajouter un nouveau canal de diffusion.

## Roadmap
 
- **1.2** : Salaires (basés sur des *Discord Actions*) + Intégration DeepL™

Rémunérer les utilisateurs sur leurs comptes virtuels en se basant sur un monitoring des activités liées à Discord & offrir un service de traduction basé sur l'API DeepL.

- **1.3** : *soon™*


## Authentification

| Parameter | Type     | Description                |
| :-------- | :------- | :------------------------- |
| `./core.cs/L:44[token]` | `string` | **Requis**. Token de bot Discord  |



## Déploiement

Snout requiert l'utilisation d'une base de données type SQLITE (*version 3*) dont le générateur est disponible dans le
dossier :
```bash
  ./SQL
```
Le runtime .NET 7.0 doit être installé sur la machine hôte.

Une fois compilé, le bot est exécuté comme un programme Win64 :
```bash
  ./snout.exe
  
  OU
  
  dotnet snout.dll 
```

## Développement

- [@RedTheFoxx](https://github.com/RedTheFoxx)

