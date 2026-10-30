import Link from "next/link";
import { ChevronLeft } from "lucide-react";
import { api } from "@/lib/api";
import { PageHeader } from "@/components/page-header";
import { AdministratorOnly } from "@/components/admin/administrator-only";
import { CategoryList } from "@/components/catalog/category-list";

export const metadata = { title: "Algemene catalogus - PakEchtMee" };

export default async function GeneralCatalogPage() {
  const me = await api.me();

  if (!me?.isAdministrator) {
    return (
      <>
        <PageHeader title="Algemene catalogus" />
        <AdministratorOnly />
      </>
    );
  }

  const categories = await api.listGeneralCategories(true);

  return (
    <>
      <header className="flex items-center gap-2 px-2 pt-4">
        <Link
          href="/beheer"
          className="text-muted-foreground hover:text-foreground flex size-11 items-center justify-center rounded-md"
          aria-label="Terug naar beheer"
        >
          <ChevronLeft aria-hidden />
        </Link>
      </header>
      <PageHeader title="Algemene catalogus" description="Wat je hier aanpast, zien alle families direct." />
      <main className="px-4">
        <CategoryList categories={categories} scope="general" />
      </main>
    </>
  );
}
