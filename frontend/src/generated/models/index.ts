/* tslint:disable */
/* eslint-disable */
// Generated by Microsoft Kiota
// @ts-ignore
import { type Parsable, type ParseNode, type SerializationWriter } from '@microsoft/kiota-abstractions';

/**
 * Creates a new instance of the appropriate class based on discriminator value
 * @param parseNode The parse node to use to read the discriminator value and create the object
 * @returns {GetExampleResponse}
 */
// @ts-ignore
export function createGetExampleResponseFromDiscriminatorValue(parseNode: ParseNode | undefined) : ((instance?: Parsable) => Record<string, (node: ParseNode) => void>) {
    return deserializeIntoGetExampleResponse;
}
/**
 * The deserialization information for the current model
 * @returns {Record<string, (node: ParseNode) => void>}
 */
// @ts-ignore
export function deserializeIntoGetExampleResponse(getExampleResponse: Partial<GetExampleResponse> | undefined = {}) : Record<string, (node: ParseNode) => void> {
    return {
        "message": n => { getExampleResponse.message = n.getStringValue(); },
    }
}
export interface GetExampleResponse extends Parsable {
    /**
     * The message property
     */
    message?: string | null;
}
/**
 * Serializes information the current object
 * @param writer Serialization writer to use to serialize this model
 */
// @ts-ignore
export function serializeGetExampleResponse(writer: SerializationWriter, getExampleResponse: Partial<GetExampleResponse> | undefined | null = {}) : void {
    if (getExampleResponse) {
        writer.writeStringValue("message", getExampleResponse.message);
    }
}
/* tslint:enable */
/* eslint-enable */