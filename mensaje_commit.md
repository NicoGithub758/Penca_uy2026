feat: arquitectura base N-Tier, EF Core y modelo de dominio completo

### 1. Resumen de Arquitectura y Base de Datos
Se estableció el esqueleto fundacional del proyecto aplicando separación de responsabilidades y el patrón de diseño N-Tier:
* División de la solución en 4 capas lógicas independientes (Web, Business, Data, Models) para garantizar bajo acoplamiento.
* Implementación del enfoque Code-First con Entity Framework Core.
* Creación de DbContext (`PencaDbContext`) y configuración de Inyección de Dependencias.
* Ajustado de despliegue: Dockerfile adaptado para compilar correctamente los 4 proyectos.
* Generación del modelo de dominio completo para Casos de Uso Críticos (Pagos, Notificaciones, Chat, Accesos, Administradores) con propiedades de navegación correspondientes usando colecciones (`ICollection<T>`) y validaciones de nulos para evitar errores o warnings de compilación.
* Migraciones generadas, validadas y aplicadas a SQL Server mediante la CLI nativa de .NET (`dotnet ef`) en mi máquina.

### 2. Decisiones de Diseño sobre el Modelo Conceptual (SAD)
Para estar alineado a los principios KISS y YAGNI exigidos en el curso, el dominio se ajustó respecto al diagrama original:
1. Eventos atados a Penca global: Se movió la relación para no duplicar partidos físicos en las instancias de cada penca.
2. Chat atado solo a Participación: Sin relación directa a PencaInstance para evitar redundancia de datos. Se accederá vía consultas LINQ (se pueden obtener todos los mensajes de una instancia de penca, obteniendo todos los que estén asociados a las participaciones cuyo pencaInstanceId sea el que queremos).
3. Puntaje como atributo: Se eliminó la tabla y pasó a ser TotalPoints en Participation (KISS).
4. Comisión como atributo: Eliminada como tabla; pasa a ser CommissionPercentage (KISS).
5. Deporte eliminado: Descartada abstracción multideporte (YAGNI); nos enfocamos exclusivamente en deportes de 2 equipos local y visitante (fut, basket, etc).
6. PlatformAdmin independiente: Al ser administrador global, no se ata con clave foránea a un Site específico.
7. Notificaciones simplificadas: Sin tabla de configuración; manejado por atributos en SiteUser.
8. Invitación y Solicitudes: Sin clave foránea a SiteUser, ya que el usuario externo aún no existe al momento de solicitar/recibir acceso.
9. Participación y Pagos: Relación corregida a 1 a N para soportar múltiples intentos de transacción en la tabla Payment.
### 3. INSTRUCCIONES
1. Bajen los cambios y muévanse a esta rama:
   `git fetch`
   `git checkout feature/arquitectura-base`

2. Configuración de Seguridad:
   No deberíamos subir contraseñas de SQL Server al repo. En el archivo `appsettings.json` dejé una connection string base. 
   Tienen que hacer clic derecho en el proyecto principal Web (`Penca_uy2026`) -> "Administrar secretos de usuario" y pegar ahí su propia Connection String con el usuario de ustedes y la contraseña de su motor local.

3. Instalar la herramienta de Entity Framework Core:
   Al menos yo, a la hora de generar las migraciones, tuve errores al usar la terminal del Visual Studio, entonces recomiendo usar una terminal (un powerShell) para no depender de esos bugs que al parecer, son propios del visual studio. Necesitan instalar el CLI de EF Core en sus máquinas para esto. Abran una terminal y tiren:
   `dotnet tool install --global dotnet-ef`

4. Creación de la Base de Datos Local:
   Abran la terminal en la raíz de la solución (donde está el .sln) y corran este comando para aplicar las migraciones a su SQL Server local:
   `dotnet ef database update --project Penca_uy2026.Data --startup-project Penca_uy2026`

Regla Arquitectónica que deberíamos definir: La separación de los 4 proyectos es estricta. Los Controladores (Web) no deberían nunca llamar a la base de datos (Data) directamente. Toda comunicación debe pasar por la capa de Negocios (Business).