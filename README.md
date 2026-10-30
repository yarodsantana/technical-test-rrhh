# Bonos de desempeño

Módulo de Recursos Humanos implementado sobre el proyecto base: SQL Server, .NET 10 / ASP.NET Core / EF Core y Angular 22 standalone. Conserva las capas Domain, Application, Infrastructure y Api.

## Requisitos

- SDK .NET 10, Node.js compatible con Angular 22 y npm.
- SQL Server 2019+ o Docker Desktop con contenedores Linux.
- Puerto 1433 para SQL, 5106 para API y 4200 para Angular.

## Inicio rápido en Windows / Docker

Con el SDK .NET 10 instalado y Docker Desktop abierto, desde la raíz del repositorio:

```powershell
Copy-Item .env.example .env
```

Editar la contraseña de ejemplo en el archivo local `.env` antes de iniciar SQL Server. El archivo está excluido de Git. Después ejecutar:

```powershell
docker compose up -d --wait
dotnet run --project backend/tools/BonusTest.DatabaseSetup -- .
```

La herramienta auxiliar conecta por TCP a SQL Server. En una base nueva ejecuta `database/01_schema_seed.sql` SIN modificarlo y luego `02_bonus.sql`. Si la base o tabla ya existen, las conserva. Una base preexistente incompleta requiere revisión manual; no se borra automáticamente.

En otra terminal, configurar la conexión con la misma contraseña local y arrancar la API:

```powershell
$env:ConnectionStrings__BonusTest = 'Server=tcp:127.0.0.1,1433;Database=BonusTest;User Id=sa;Password=TU_PASSWORD_LOCAL;TrustServerCertificate=True;'
dotnet run --project backend/src/BonusTest.Api --launch-profile http
```

En otra terminal:

```powershell
cd frontend
npm ci
npm start
```

- Aplicación: http://localhost:4200/bonuses
- Swagger: http://localhost:5106/swagger

La contraseña se configura por entorno y no se incorpora al código. Al terminar, `docker compose stop` conserva los datos.

### SQL Server existente

Ejecutar `database/01_schema_seed.sql` una sola vez y `database/02_bonus.sql` después con el cliente SQL de preferencia. Configurar la conexión antes de arrancar la API:

```powershell
$env:ConnectionStrings__BonusTest = 'Server=TU_SERVIDOR;Database=BonusTest;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet run --project backend/src/BonusTest.Api --launch-profile http
```

## Verificación

Detener la API antes de recompilar para evitar bloqueos de DLL en Windows. Desde la raíz del repositorio:

```powershell
dotnet test backend/BonusTest.slnx --configuration Release
cd frontend
npm ci
npm run build
npm test
```

Verificaciones ejecutadas: compilación Release .NET y producción Angular correctas; 39 pruebas xUnit y 6 de vista previa aprobadas. También se realizaron 37 comprobaciones de API sobre SQL Server real y recorridos de navegador con registro, filtros, eliminación lógica y errores de negocio. Las pruebas unitarias de servicio usan un repositorio falso y un reloj fijo; se distinguen de las comprobaciones de integración.

La base local de desarrollo conserva registros históricos anulados y eliminados lógicamente de esas comprobaciones. No se incluyen datos de esas ejecuciones en el código entregado; la base nueva parte del seed original.

## Endpoints

| Método | Ruta | Resultado |
|---|---|---|
| GET | `/api/v1/Bonus` | Filtros y `PagedResponse<BonusDto>` |
| GET | `/api/v1/Bonus/employee/{departmentCode}/{employeeNumber}` | Empleado activo y datos para el formulario |
| POST | `/api/v1/Bonus` | 201 con identificador y montos definitivos |
| PUT | `/api/v1/Bonus/{bonusId}/cancel` | 204; Registrado a Anulado |
| DELETE | `/api/v1/Bonus/{bonusId}` | 204; eliminación lógica desde Registrado |

Filtros opcionales: `departmentCode`, `employeeNumber`, `period` (YYYY-MM), `stateCode`. Paginación heredada: `pageIndex` (desde 1) y `pageSize` (10 por defecto, máximo 50). La respuesta conserva el contrato base `pageNumber`, `pageSize`, `totalPages`, `totalRecords`, `data`.

Ejemplo de registro; enviar header `UserCode: candidato`:

```json
{
  "departmentCode": "TECH1",
  "employeeNumber": 101,
  "period": "2026-10",
  "bonusTypeId": 2,
  "value": 10,
  "reasonCode": "000001"
}
```

Resultado del ejemplo: bruto 500.00, retención 40.00 y neto 460.00. Usar el mes actual o uno anterior al probar.

## Decisiones técnicas

- **Capas:** Domain contiene cálculo y transiciones; Application orquesta validaciones y casos de uso; Infrastructure implementa EF/SQL; Api traduce HTTP. Los controladores no contienen reglas de negocio.
- **Dinero:** `decimal` en .NET, escalas explícitas en SQL, `MidpointRounding.AwayFromZero` a 2 decimales. Se redondea el bruto; la retención se calcula sobre ese bruto y se redondea; el neto es la diferencia. Un valor positivo muy pequeño puede redondearse a cero: no se inventa un mínimo de pago.
- **Valor ingresado:** hasta cuatro decimales (`decimal(20,4)`); se rechaza precisión adicional para evitar truncamientos silenciosos. La UI muestra el valor con hasta cuatro decimales y el símbolo % cuando aplica.
- **Histórico:** se guardan sueldo, tasa e importes del momento del registro. Los cambios futuros en el empleado no recalculan bonos existentes.
- **Periodo:** se almacena como `date` en el primer día del mes. El mes actual se determina en Lima (UTC-05). La auditoría se guarda en UTC.
- **Auditoría:** `UserCode` obligatorio, recortado y hasta 100 caracteres. Al crear, se inicializan tanto creación como modificación. No se presenta como autenticación: se reutiliza el mecanismo de la prueba.
- **Integridad:** FK compuesta a Employee y FK a tipo, motivo y estado; restricciones CHECK para valores, estados e importes. `IsDeleted` no elimina físicamente el registro.
- **Duplicados concurrentes:** validación previa para dar un mensaje claro e índice único filtrado `(DepartmentCode, EmployeeNumber, Period)` sobre bonos no eliminados y distintos de Anulado. Errores SQL 2601/2627 se traducen a BusinessException 409.
- **Cambios concurrentes:** `rowversion` impide que dos operaciones que leyeron la misma versión se sobreescriban; el conflicto devuelve 409.
- **Listado:** consulta sin tracking, filtros en SQL, CountAsync y Skip/Take antes de materializar. Orden estable por periodo e identificador descendentes; las descripciones viajan listas para mostrar. El segundo índice favorece el orden por periodo y el filtrado de estado sin filas eliminadas.
- **Angular:** formularios reactivos y señales, modal nativo, estados de carga y confirmación. La búsqueda de empleado se invalida al cambiar su clave. La vista previa usa aritmética entera con BigInt para evitar errores binarios en mitades; la API recalcula y es la autoridad.
- **Errores:** se conserva BusinessException y el interceptor 409 del proyecto base. Los componentes no duplican mensajes de error.
- **Dependencias:** no se agregó librería de UI ni arquitectura adicional.

## Alcance y pendientes

- Implementado el flujo obligatorio de búsqueda, registro, listado paginado, anulación y eliminación lógica.
- Extra implementado: pruebas unitarias de cálculo, estados y casos de uso con xUnit.
- Extra pendiente: pago y distribución por proyecto. No hay botón ni endpoint de pago anunciado como funcional.
- Fuera del alcance solicitado: autenticación real y despliegue productivo. Se conserva el header del proyecto base.

## Uso de IA y aprendizaje

Se utilizó IA generativa mediante Codex para analizar el enunciado, proponer el diseño, generar código, preparar pruebas y revisar casos límite. Las decisiones y limitaciones están documentadas. No se considera que código generado sea correcto hasta revisarlo y verificarlo.

Referencias de preparación del entorno: [SDK por script oficial](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script) y [SQL Server en Docker](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver16).
