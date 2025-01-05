import { AnonymousAuthenticationProvider, ParseNodeFactory, ParseNodeFactoryRegistry, type SerializationWriterFactory } from "@microsoft/kiota-abstractions";
import { FetchRequestAdapter, HeadersInspectionHandler, KiotaClientFactory, ParametersNameDecodingHandler, RedirectHandler, RetryHandler, UserAgentHandler } from "@microsoft/kiota-http-fetchlibrary";
import { createApiClient } from "./generated/apiClient";
import { JsonParseNodeFactory, JsonSerializationWriterFactory } from "@microsoft/kiota-serialization-json";
import {
    SerializationWriterFactoryRegistry
} from "@microsoft/kiota-abstractions/dist/es/src/serialization/serializationWriterFactoryRegistry";

const authProvider = new AnonymousAuthenticationProvider();

const localParseNodeFactory: ParseNodeFactoryRegistry = new ParseNodeFactoryRegistry();
const jsonParseNodeFactory: ParseNodeFactory = new JsonParseNodeFactory();
localParseNodeFactory.contentTypeAssociatedFactories.set(jsonParseNodeFactory.getValidContentType(), jsonParseNodeFactory);

const localSerializationWriterFactory: SerializationWriterFactoryRegistry = new SerializationWriterFactoryRegistry();
const jsonSerializer: SerializationWriterFactory = new JsonSerializationWriterFactory();

localSerializationWriterFactory.contentTypeAssociatedFactories.set(jsonSerializer.getValidContentType(), jsonSerializer);

const http = KiotaClientFactory.create((url, requestInit) => {
    return fetch(url, {
        ...requestInit,
        credentials: 'include',
    });
}, [
    new RetryHandler(),
    new RedirectHandler(),
    new ParametersNameDecodingHandler(),
    new UserAgentHandler(),
    new HeadersInspectionHandler()
]);

const adapter = new FetchRequestAdapter(
    authProvider,
    localParseNodeFactory,
    localSerializationWriterFactory,
    http);

adapter.baseUrl = import.meta.env.VITE_BACKEND_URL;

export const apiClient = createApiClient(adapter);