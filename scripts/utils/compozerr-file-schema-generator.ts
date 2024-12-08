import { zodToJsonSchema } from 'npm:zod-to-json-schema';
import { CompozerrFile } from '../types/compozerr-file.ts';

export const exportCompozerrSchemaAsync = async () => {
    const jsonSchema = zodToJsonSchema(CompozerrFile);

    await Deno.writeFile(
        'compozerr.schema.json',
        new TextEncoder().encode(JSON.stringify(jsonSchema, null, 2))
    );
}
