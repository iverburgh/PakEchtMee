import { api } from "@/lib/api";
import { PageHeader } from "@/components/page-header";
import { NoFamilyNotice } from "@/components/no-family-notice";
import { FamilyCompositionCard } from "@/components/family/family-composition-card";

export const metadata = { title: "Gezin - PakEchtMee" };

export default async function FamilyPage() {
  const me = await api.me();

  if (me && !me.family) {
    return (
      <>
        <PageHeader title="Gezin" />
        <main className="px-4">
          <NoFamilyNotice />
        </main>
      </>
    );
  }

  const composition = await api.getFamilyComposition();

  return (
    <>
      <PageHeader title="Gezin" description={me?.family ? `Familie ${me.family.name}` : undefined} />
      <main className="px-4">
        <FamilyCompositionCard composition={composition} />
      </main>
    </>
  );
}
