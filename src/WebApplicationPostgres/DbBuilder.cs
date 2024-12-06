public static class DbBuilder
{
    public static async Task Create(NpgsqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            create table IF NOT EXISTS public."Companies"
            (
                "Id" uuid not null
                    constraint "PK_Companies"
                        primary key,
                "Content" text
            );
            
            alter table public."Companies"
                owner to postgres;
            
            create table IF NOT EXISTS public."Employees"
            (
                "Id" uuid    not null
                    constraint "PK_Employees"
                        primary key,
                "CompanyId" uuid    not null
                    constraint "FK_Employees_Companies_CompanyId"
                        references public."Companies"
                        on delete cascade,
                "Content"   text,
                "Age"       integer not null
            );
            
            alter table public."Employees"
                owner to postgres;
            
            create index IF NOT EXISTS "IX_Employees_CompanyId"
                on public."Employees" ("CompanyId");
            
            """;
        await command.ExecuteNonQueryAsync();
    }
}