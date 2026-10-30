import Link from "next/link";
import { ChevronRight, Globe } from "lucide-react";
import { api } from "@/lib/api";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/page-header";
import { AdministratorOnly } from "@/components/admin/administrator-only";
import { FamilyAdministration } from "@/components/admin/family-administration";

export const metadata = { title: "Beheer - PakEchtMee" };

export default async function AdministrationPage() {
  const me = await api.me();

  if (!me?.isAdministrator) {
    return (
      <>
        <PageHeader title="Beheer" />
        <AdministratorOnly />
      </>
    );
  }

  const families = await api.listFamilies();

  return (
    <>
      <PageHeader title="Beheer" description="Families, leden en de algemene catalogus." />
      <main className="flex flex-col gap-6 px-4">
        <Link href="/beheer/catalogus" className="rounded-xl">
          <Card className="hover:bg-accent/50 flex min-h-16 flex-row items-center gap-3 p-4 transition-colors motion-reduce:transition-none">
            <Globe className="text-primary size-5 shrink-0" aria-hidden />
            <div className="flex min-w-0 flex-1 flex-col">
              <span className="font-medium">Algemene catalogus</span>
              <span className="text-muted-foreground text-xs">Categorieën en spullen die elke familie kan gebruiken.</span>
            </div>
            <ChevronRight className="text-muted-foreground size-5 shrink-0" aria-hidden />
          </Card>
        </Link>

        <section className="flex flex-col gap-3">
          <h2 className="text-lg font-semibold tracking-tight">Families</h2>
          <FamilyAdministration families={families} />
        </section>
      </main>
    </>
  );
}
