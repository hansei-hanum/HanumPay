namespace Models.Responses;

/// <summary>
/// 결제 응답 DTO
/// </summary>
public class PaymentResponse {
    /// <summary>
    /// 송금자 잔고
    /// </summary>
    public ulong BalanceAmount { get; set; }
    /// <summary>
    /// 결제 트랜잭션 정보
    /// </summary>
    public required TransactionInfo Transaction { get; set; }
}